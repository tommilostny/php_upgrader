<?php
/* Overwrite of mssql_* functions with their sqlsrv_* equivalents for compatibility with PHP 7.0+
 * Author: Tomáš Milostný, 2023
 */

define('MSSQL_ASSOC', SQLSRV_FETCH_ASSOC);
define('MSSQL_NUM', SQLSRV_FETCH_NUMERIC);
define('MSSQL_BOTH', SQLSRV_FETCH_BOTH);
define('MSSQL_DEBUG', false);

if (MSSQL_DEBUG) {
    error_reporting(E_ALL);
    ini_set('display_errors', "1");
}

/** Global connection variable used when the $conn parameter is null. */
$_mssql_overwrite_conn = null;

/** Checks if the $conn parameter is null and if so, assigns the global connection to it. */
function _check_assign_from_global_conn(&$conn) {
    if ($conn === null) {
        global $_mssql_overwrite_conn;
        $conn = $_mssql_overwrite_conn ?? false;
    }
}

function mssql_connect($serverInfo, $username, $password, $database) {
    // Replace ':' with ', ' in server info to maintain backward compatibility.
    $serverInfo = str_replace(':', ', ', $serverInfo);
    // Construct the connection options array
    $connectionOptions = [
        "Database" => $database,
        "UID" => $username,
        "PWD" => $password,
        "TrustServerCertificate" => true,
    ];

    if (MSSQL_DEBUG) {
        $connectionOptions['TraceOn'] = true;
        $connectionOptions['TraceFile'] = '__mssql_trace.log';

        echo '<pre>$serverInfo = ';
        var_dump($serverInfo);
        echo '</pre>';
        echo '<pre>$connectionOptions = ';
        var_dump($connectionOptions);
        echo '</pre><br>';
    }

    global $_mssql_overwrite_conn;
    $_mssql_overwrite_conn = sqlsrv_connect($serverInfo, $connectionOptions);
    
    if (MSSQL_DEBUG && file_exists($connectionOptions['TraceFile'])) {
        $log = file_get_contents($connectionOptions['TraceFile']);
        echo "<pre><b>Trace</b>: $log</pre><br>";
        unlink($connectionOptions['TraceFile']);
    }

    return $_mssql_overwrite_conn;
}

function mssql_close($conn = null) {
    _check_assign_from_global_conn($conn);
    if ($conn === false) {
        return false;
    }
    return sqlsrv_close($conn);
}

function mssql_query($query, $conn = null) {
    _check_assign_from_global_conn($conn);
    if ($conn === false) {
        return false;
    }
    return sqlsrv_query($conn, $query);
}

function mssql_result($result, $row, $field) {
    // Move the result pointer to the desired row
    for ($i = 0; $i < $row; $i++) {
        sqlsrv_fetch($result);
    }
    // Fetch the specified field's value
    $row = sqlsrv_fetch_array($result, SQLSRV_FETCH_BOTH);
    if ($row === false || $row === null) {
        return false;
    }
    return $row[$field];
}

function mssql_fetch_assoc($result) {
    if ($result === false) {
        return false;
    }
    return sqlsrv_fetch_array($result, SQLSRV_FETCH_ASSOC);
}

function mssql_fetch_array($result, $resultType = SQLSRV_FETCH_BOTH) {
    if ($result === false) {
        return false;
    }
    return sqlsrv_fetch_array($result, $resultType);
}

function mssql_fetch_object($result, $className = 'stdClass') {
    if ($result === false) {
        return false;
    }
    return sqlsrv_fetch_object($result, $className);
}

function mssql_get_last_message() {
    $errors = sqlsrv_errors(SQLSRV_ERR_ALL);
    if ($errors === null) {
        return null;
    }
    $lastError = end($errors);
    if ($lastError === false) {
        return null;
    }
    return $lastError['message'];
}

function mssql_num_rows($result) {
    if ($result === false) {
        return 0;
    }
    $numRows = sqlsrv_num_rows($result);
    return ($numRows !== false) ? $numRows : 0;
}
?>
