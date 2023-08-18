<?php
/* Overwrite of mssql_* functions with their sqlsrv_* equivalents for compatibility with PHP 7.0+
 * Author: Tomáš Milostný, 2023
 */

 function mssql_connect($serverInfo, $username, $password, $database) {
     // Replace ':' with ',' in server info
    $serverInfo = str_replace(':', ',', $serverInfo);
    // Construct the connection options array
    $connectionOptions = array(
        "Database" => $database,
        "UID" => $username,
        "PWD" => $password,
        "TrustServerCertificate" => true,
    );
    // Establish the connection
    $conn = sqlsrv_connect($serverInfo, $connectionOptions);
    return $conn;
}

function mssql_close($conn) {
    return sqlsrv_close($conn);
}

function mssql_query($query, $conn) {
    $result = sqlsrv_query($conn, $query);
    return $result;
}

function mssql_result($result, $row, $field) {
    // Move the result pointer to the desired row
    for ($i = 0; $i < $row; $i++) {
        sqlsrv_fetch($result);
    }
    // Fetch the specified field's value
    $row = sqlsrv_fetch_array($result, SQLSRV_FETCH_ASSOC);
    return $row[$field];
}

function mssql_fetch_assoc($result) {
    return sqlsrv_fetch_array($result, SQLSRV_FETCH_ASSOC);
}

function mssql_fetch_array($result) {
    return sqlsrv_fetch_array($result, SQLSRV_FETCH_BOTH);
}

function mssql_fetch_object($result, $className = "stdClass") {
    $row = sqlsrv_fetch_object($result, $className);
    return $row;
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
    $numRows = sqlsrv_num_rows($result);
    return ($numRows !== false) ? $numRows : 0;
}
?>
