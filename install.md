# Kurulum Rehberi

Windows bilgisayarda uygulamayı çalıltırma adımları:

---

## 1. Ön Gereksinimler

- **.NET 8.0 Runtime**  
  Uygulama, .NET 8.0 üzerinde çalışır. [Resmi .NET 8.0 İndirme Sayfası](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) üzerinden .NET 8.0 Runtime’ı indirip yükleyin.

- **Zebra ZC350 Yazıcı Sürücüleri**  
  Yazıcınızın en güncel sürücülerini yüklediğinizden emin olun. Sürücüler için [Zebra ZC350](https://www.zebra.com/tr/tr/support-downloads/printers/card/zc350.html) sayfasını ziyaret edebilirsiniz.

---

## 2. Dosya Yapısı

- **publish** klasörü: Derlenmiş çalıştırılabilir dosyayı (`ZebraPrinterCLI.exe`) içerir.
- **ZebraPrinterCLI** klasörü: Uygulamanın kaynak dosyalarını ve ayarlarını barındırır.

---

## 3. Uygulamayı Çalıştırma

### Adım 1: Uygulamayı Başlatın

1. **Dosya Gezgini ile Gitmek**  
   `\publish` dizinine gidin.

2. **Uygulamayı Çalıştırın**  
   `ZebraPrinterCLI.exe` dosyasına çift tıklayarak uygulamayı başlatın.

   Uygulama başlatıldığında aşağıdaki URL’lerde dinlemeye başlayacaktır:
   - **HTTPS:** `https://0.0.0.0:53039`
   - **HTTP:** `http://0.0.0.0:53040`

### Adım 2: API Arayüzünü Kontrol Edin

Tarayıcınızı açın ve aşağıdaki adreslerden birine gidin:
- [http://localhost:53040](http://localhost:53040)  
  veya  
- [https://localhost:53039](https://localhost:53039)

Swagger arayüzü açılarak API’nin interaktif dokümantasyonunu görüntüleyebilirsiniz.


