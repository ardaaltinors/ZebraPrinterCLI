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
        "serialNumber": "JWL134836",
        "gemstone": "Lab Diamond (GH, SI+)",
        "material": "18k White Gold",
        "totalCarat": "2.25 ct.",
        "date": "9/4/2024",
        "qrcode": "altinors.com"
    }
}
```


