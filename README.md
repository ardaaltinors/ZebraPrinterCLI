# Zebra ZC350 Automated Print API

## Adjust settings

Change USB or Network discovery settings from `appsettings.json`
```json
"PrinterConfig": {
    "EnableUsbDiscovery": true, // true or false
    "EnableNetworkDiscovery": false, // true or false (default is false)
}
```

## Endpoints

### Get Printers

```bash
GET http://localhost:53039/printers
```

### Print

```bash
POST http://localhost:53039/print
```
```json
{
    "FieldData": {
        "serialNumber": "1234567890",
        "gemstone": "Diamond",
        "material": "Gold",
        "totalCarat": "1.00",
        "date": "2024-01-01"
    }
}
```

