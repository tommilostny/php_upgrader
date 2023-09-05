<?php
/** McGoPay class helper for migration from SOAP GoPay API to REST API.
  * @author Tomáš Milostný
  * @date 2023-08-30
  */

use GoPay\Definition\Payment\PaymentInstrument;
use GoPay\Definition\Response\PaymentStatus;

class McGoPayHelper {
    private McGoPay $mcgopay;
    
    public function __construct(?int $domainId) {
        $this->mcgopay = new McGoPay($domainId, production);
    }

    public static function paymentMethodName(int $id): string {
        switch ($id) {
            case 12: return 'Platba kartou';
            case 666: return 'Platba bankovním převodem';
            case 667: case 700: return 'PayPal';
            case 701: return 'GoPay';
            case 702: return 'Bitcoin';
            case 703: return 'MasterPass';
            case 705: return 'paysafecard';
            case 706: return 'Google Pay';
            case 707: return 'Apple Pay';
            default: return "Neznámá platba: $id";
        }
    }
    
    public static function paymentMethodImage(int $id): string {
        $basePath = '/gopay/img/icons-basket';
        switch ($id) {
            case 12: return "$basePath/_gp_online2.png";
            case 666: return "$basePath/_gp_bank2.png";
            case 667: case 700: return "$basePath/_gp_paypal2.png";
            case 701: return "$basePath/gopay1.png";
            case 702: return "$basePath/_gp_bitcoin.png";
            case 703: return "$basePath/_gp_masterpass.jpg";
            case 705: return "$basePath/_gp_paysafe.jpg";
            case 706: return "$basePath/_gp_gpay.png";
            case 707: return "$basePath/apple.png";
            default: return "$basePath/gopay1.png";
        }
    }

    public function isGopay(int $paymentId): bool {
        return in_array($paymentId, $this->mcgopay->getOnline());
    }

    public function renderPaymentMethods(int $selectedId): void {
        ?> <h3 class="formular_podnadpis">On-line platby</h3> <?php
        $onlineMethods = $this->mcgopay->getOnline();

        foreach ($onlineMethods as $paymentId): ?>
            <div id="platba_<?= $paymentId ?>" class="krok3_radek_prava platba">
                <div class="kro3_prava_radio_logo">
                    <input class="krok3_radio_prava"
                            type="radio"
                            id="payment_<?= $paymentId ?>"
                            name="card_form_adress_platba"
                            onclick="set_hide('add','')"
                            value="<?= $paymentId ?>"
                            <?= $paymentId == $selectedId ? 'checked' : '' ?> />
                    <label for="payment_<?= $paymentId ?>" class="krok3_logo">
                        <img src="<?= self::paymentMethodImage($paymentId) ?>" alt="gopay" />
                    </label>
                </div>
                <div class="krok3_prava_text_cena">
                    <span class="krok3_prava_obal_text krok3_1_radek">
                        <label for="payment_<?= $paymentId ?>" class="krok3_prava_text1">
                            <?= self::paymentMethodName($paymentId) ?>
                        </label>
                    </span>
                    <span class="krok3_prava_cena">0 <?= LNG_GLOBAL_02_MENA ?></span>
                </div>
                <div class="spacer"></div>
            </div>
            <?php
        endforeach; ?>
        <input type="hidden" name="GoPayCodes" value="<?= implode('*',$onlineMethods) ?>" />
        <?php
    }

    public function createPayment(string $doklad, int $paymentId, int $orderId, array $contact): ?string {
        if ($this->isGopay($paymentId)) {
            $items = $this->mcgopay->getItemsFromDB($orderId);
            $gopaySwift = null;
            switch ($paymentId) {
                case 666:
                    $gopayMethod = PaymentInstrument::BANK_ACCOUNT;
                    $gopaySwift = 'ALL_ONLINE';
                    break;
                case 667: case 700: $gopayMethod = PaymentInstrument::PAYPAL; break;
                case 706: $gopayMethod = PaymentInstrument::GPAY;             break;
                case 707: $gopayMethod = PaymentInstrument::APPLE_PAY;        break;
                default:  $gopayMethod = PaymentInstrument::PAYMENT_CARD;     break;
            }
            $response = $this->mcgopay->createPayment2($doklad, $contact, $items, $gopayMethod, $gopaySwift, null, $orderId);
            return $this->mcgopay->getActionUrl($response);
        }
        return null;
    }

    public function renderPaymentButton(string $gatewayLink): void {
        ?><form class="text-center" action="<?= $gatewayLink ?>" method="POST" id="gopay-payment-button">
            <button class="kosik_zpet" id="payment-invoke-checkout" type="submit" style="height:100%">
                Zaplatit nyní
            </button>
            <script type="text/javascript" src="<?= $this->mcgopay->getSctiptUrl(); ?>"></script>
        </form><?php
    }

    public function renderPaymentStatus(): bool {
        if (isset($_GET['id'])) $id = $_GET['id'];
        elseif (isset($_GET['order'])) $id = $this->mcgopay->getPaymentIDbyOrder($_GET['order']);
        elseif (isset($_GET['doklad_n'])) $id = $this->mcgopay->getPaymentIDbyOrder($_GET['doklad_n']);
        elseif (isset($_GET['orderid'])) $id = $this->mcgopay->getPaymentIDbyOrderID($_GET['orderid']);

        if (!isset($id)) {
            return false;
        }

        $response = $this->mcgopay->getStatus($id);
        if(!$this->mcgopay->isResponseOK($response)) { ?>
            <div class="text-center">
                <table>
                    <tr>
                        <td width="175px" class="resp_table_500">
                            <img src="/gopay/img/icons-basket/_gp_cancel.jpg" />
                        </td>
                        <td class="resp_table_500">
                            <p><strong>Nastala chyba při platbě pomocí GoPay.</strong></p>
                        </td>
                    </tr>
                </table>
            </div>
        <?php
        } else {
            $this->mcgopay->setOrderStatus($response);
            switch ($this->getPaymentStatus($response)) {
                default:
                case PaymentStatus::PAYMENT_METHOD_CHOSEN:
                case PaymentStatus::CREATED: ?>
                    <div class="text-center">
                        <table width="100%">
                            <tr>
                                <td width="175px" class="resp_table_500">
                                    <img src="/gopay/img/icons-basket/_gp_nopaid.jpg"/>
                                </td>
                                <td class="resp_table_500">
                                    <p class="gp_label gp_red"><b>GoPay: Nezaplaceno</b></p>
                                    <p><strong>Pozor:</strong> - Zboží nebylo zaplaceno.</p>
                                    <?php $this->renderPaymentButton($this->mcgopay->getActionUrl($response)) ?>
                                </td>
                            </tr>
                        </table>
                    </div>
                    <?php break;
                case PaymentStatus::PAID: ?>
                    <div class="text-center">
                        <table width="100%">
                            <tr>
                                <td width="175px" class="resp_table_500">
                                    <img src="/gopay/img/icons-basket/_gp_ok.jpg"/>
                                </td>
                                <td class="resp_table_500">
                                    <p class="gp_label gp_green"><b>GoPay: Zaplaceno</b></p>
                                    <p>Zboží bylo zaplaceno.</p>
                                </td>
                            </tr>
                        </table>
                    </div>
                    <?php break;
                case PaymentStatus::TIMEOUTED:
                case PaymentStatus::REFUNDED:
                case PaymentStatus::PARTIALLY_REFUNDED:
                case PaymentStatus::CANCELED: ?>
                    <div class="text-center">
                        <table width="100%">
                            <tr>
                                <td width="175px" class="resp_table_500">
                                    <img src="/gopay/img/icons-basket/_gp_cancel.jpg"/>
                                </td>
                                <td class="resp_table_500">
                                    <p class="gp_label gp_red"><b>Storno</b></p>
                                    <p>Zboží bylo stornováno nebo vráceno.</p>
                                </td>
                            </tr>
                        </table>
                    </div>
                <?php break;
            }
        }
        return true;
    }

    public function renderPaymentStatusAdminDetail(int $orderId, string $paymentName): void {
        ?><tr>
            <th><strong>GoPay</strong></th>
            <td style="padding-top: 10px">
                <?= $paymentName ?>
                <br />
                <?php
                $this->renderPaymentStatusAdmin($orderId);
                ?>
            </td>
        </tr>
        <?php
    }

    public function renderPaymentStatusAdminList(int $orderId): void {
        $this->renderPaymentStatusAdmin($orderId);
        ?><br /><?php
    }

    private function renderPaymentStatusAdmin(int $orderId): void {
        $id = $this->mcgopay->getPaymentIDbyOrderID($orderId);
        $response = $this->mcgopay->getStatus($id);
        $pay_status = $this->mcgopay->isResponseOK($response) ? $this->getPaymentStatus($response) : null;           
        switch ($pay_status) {
            case PaymentStatus::PAID: ?>
                <span class="color_bluen"><strong>Platba zaplacena</strong> dne <?= $this->mcgopay->getOrderStatus($orderId)['payment_time'] ?></span>
                <?php break;
            case PaymentStatus::CANCELED: ?>
                <span class="color_orange"><strong>Platba byla zrušena</strong></span>
                <?php break;
            case PaymentStatus::TIMEOUTED: ?>
                <span class="color_orange"><strong>Vypršel časový limit na platbu</strong></span>
                <?php break;
            case PaymentStatus::REFUNDED: ?>
                <span class="color_orange"><strong>Platba byla vrácena (reklamace)</strong></span>
                <?php break;
            default: ?>
                <span class="red"><strong>NEZAPLACENO</strong></span>
                <?php break;
        }
    }

    private function getPaymentStatus($response): string {
        $pay_status = $this->mcgopay->getStatusID($response);
    
        $is_canceled = Database::selectFetch("
            SELECT
                (min(stav) = '6')::integer AS is_canceled
            FROM
                orders JOIN orders_data
                    ON orders_data.order_id = orders.order_id
                    AND orders.doklad_n = ?
            ;",
            [ $response->json['order_number'] ]
        )->is_canceled;
    
        if (!$is_canceled && in_array($pay_status, [ PaymentStatus::TIMEOUTED, PaymentStatus::CANCELED ])) {
            $respTimeout = $this->mcgopay->getNewPaymentFromTimeout($response);
            if ($this->mcgopay->isResponseOK($respTimeout)) {
                $response = $respTimeout;
                $pay_status = $this->mcgopay->getStatusID($response);
            }
        }
        return $is_canceled ? PaymentStatus::CANCELED : $pay_status;
    }
}
