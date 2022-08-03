namespace PhpUpgrader.Rubicon.UpgradeExtensions;

public static class AegisxHomeTopProducts
{
    /// <summary> Úprava SQL dotazu na top produkty v souboru aegisx\home.php. </summary>
    public static FileWrapper UpgradeHomeTopProducts(this FileWrapper file)
    {
        if (file.Path.EndsWith(Path.Join("aegisx", "home.php"), StringComparison.Ordinal))
        {
            file.Content.Replace("SELECT product_id, COUNT(orders_data.order_id) AS num_orders FROM orders_data, orders WHERE orders_data.order_id=orders.order_id AND \" . getSQLLimit3Months() . \" GROUP BY product_id ORDER BY num_orders DESC LIMIT 1",
                "SELECT product_id, COUNT(orders_data.order_id) AS num_orders FROM orders_data, orders WHERE orders_data.order_id=orders.order_id AND \" . getSQLLimit3Months() . \" AND product_id IN (SELECT product_id FROM product_info) GROUP BY product_id ORDER BY num_orders DESC LIMIT 1");
        }
        return file;
    }
}
