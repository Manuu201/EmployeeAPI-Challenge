$baseUrl = "http://localhost:8080"
$email = "race.email.$([DateTime]::UtcNow.ToString('yyyyMMddHHmmssfff'))@example.com"
$requestCount = 10

# Solicita el JWT sin mostrarlo en pantalla.
$secureToken = Read-Host "Pega el JWT sin la palabra Bearer" -AsSecureString
$tokenPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)

try {
    $token = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($tokenPointer)
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($tokenPointer)
}

# Todas las solicitudes esperaran hasta la misma hora antes de comenzar.
$startAt = [DateTime]::UtcNow.AddSeconds(10)

$jobs = foreach ($requestNumber in 1..$requestCount) {
    Start-Job -ArgumentList @(
        $requestNumber,
        $baseUrl,
        $email,
        $token,
        $startAt
    ) -ScriptBlock {
        param(
            $requestNumber,
            $baseUrl,
            $email,
            $jwt,
            $startAt
        )

        $headers = @{
            Authorization = "Bearer $jwt"
        }

        $body = @{
            name       = "Prueba concurrencia email"
            email      = $email
            department = "TI"
            position   = "Developer"
        } | ConvertTo-Json -Compress

        while ([DateTime]::UtcNow -lt $startAt) {
            Start-Sleep -Milliseconds 10
        }

        try {
            $response = Invoke-WebRequest `
                -Uri "$baseUrl/api/employee" `
                -Method Post `
                -Headers $headers `
                -ContentType "application/json" `
                -Body $body `
                -UseBasicParsing

            [PSCustomObject]@{
                Request = $requestNumber
                Status  = [int]$response.StatusCode
                Body    = $null
            }
        }
        catch {
            $statusCode = 0

            if ($null -ne $_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }

            $errorBody = $_.ErrorDetails.Message

            if ([string]::IsNullOrWhiteSpace($errorBody)) {
                $errorBody = $_.Exception.Message
            }

            # Oculta el JWT si una respuesta de error incluye los headers.
            if (-not [string]::IsNullOrWhiteSpace($jwt)) {
                $errorBody = $errorBody.Replace($jwt, "[JWT REDACTADO]")
            }

            [PSCustomObject]@{
                Request = $requestNumber
                Status  = $statusCode
                Body    = $errorBody
            }
        }
    }
}

$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

foreach ($result in ($results | Sort-Object Request)) {
    $statusDescription = switch ($result.Status) {
        201 { "Created" }
        409 { "Conflict" }
        500 { "Internal Server Error" }
        0   { "Sin respuesta HTTP" }
        default { "Otro resultado" }
    }

    Write-Host ("Request #{0}: HTTP {1} {2}" -f `
        $result.Request,
        $result.Status,
        $statusDescription
    )

    if ($result.Status -ne 201 -and -not [string]::IsNullOrWhiteSpace($result.Body)) {
        Write-Host ("  Body: {0}" -f $result.Body)
    }
}

# Consulta cuantos empleados quedaron persistidos con el email de prueba.
Write-Host ""
Write-Host "Consultando empleados persistidos con el email: $email"

$headers = @{
    Authorization = "Bearer $token"
}

try {
    $employees = Invoke-RestMethod `
        -Uri "$baseUrl/api/employee" `
        -Method Get `
        -Headers $headers

    $matchingEmployees = @(
        $employees | Where-Object {
            $_.email -eq $email
        }
    )

    Write-Host ("Employees encontrados: {0}" -f $matchingEmployees.Count)

    if ($matchingEmployees.Count -gt 0) {
        $matchingEmployees |
            Select-Object id, name, email, department, position |
            Format-Table -AutoSize
    }
}
catch {
    Write-Host "No fue posible consultar los empleados."
    Write-Host ("Error: {0}" -f $_.Exception.Message)
}

# Elimina el token de las variables principales al finalizar.
$token = $null
$headers = $null
$secureToken.Dispose()
