# InterFullMarkt: Yapay Zekâ Destekli Kurumsal Web Ekosistemi

> Yeni Nesil, Ölçeklenebilir ve AI-Driven B2B/B2C Platform Çözümü

---

## 📌 Proje Bağlamı ve Vizyon

**InterFullMarkt**, **BTK Akademi** ve **Samsun Teknopark** iş birliğiyle hayata geçirilen *"Yapay Zekâ Destekli ASP.NET Core Web Uygulamaları Geliştirme Atölyesi"* kapsamında, endüstri standartlarında bir mühendislik yaklaşımıyla tasarlanmış kurumsal bir web ekosistemidir. 

Bu proje, geleneksel e-ticaret ve pazar yeri kavramlarının ötesine geçerek; modern yazılım mimarisi prensiplerini (*Clean Architecture, SOLID, Design Patterns*) ve yapay zekâ entegrasyonlarını harmanlayan, yüksek performanslı ve vizyoner bir altyapı sunmayı hedeflemektedir.

## 🎓 Akademik Rehberlik ve Teşekkür

Bu projenin fikri temellerinin atılmasından, mimari kararların şekillenmesine ve geliştirme sürecinin her aşamasına kadar bizlere eşsiz akademik vizyonuyla liderlik eden **Prof. Dr. Zafer Cömert** hocamıza en derin şükranlarımızı sunarız. Atölye boyunca sağladığı değerli akademik katkılar ve sektörel rehberlik, projenin yenilikçi bir teknoloji ürününe dönüşmesinde temel taş olmuştur.

---

## 🚀 Proje Tanımı

**InterFullMarkt**, standart bir pazar yeri uygulamasından ziyade; **ölçeklenebilirlik (Scalability)**, **yüksek erişilebilirlik (High Availability)** ve veri odaklı karar mekanizmaları üzerine inşa edilmiş, yapay zekâ destekli analizler sunan bir **ASP.NET Core** çözümüdür. 

Platform, karmaşık iş süreçlerini optimize etmek, kullanıcı deneyimini zenginleştirmek ve büyük veri yığınlarını anlamlı içgörülere dönüştürmek amacıyla *AI-Assisted Development* metodolojileriyle donatılmıştır.

---

## 🏗 Teknik Mimari (Architectural Excellence)

Proje, sürdürülebilirlik, test edilebilirlik ve modülerlik ilkeleri gözetilerek **N-Layer Architecture (Çok Katmanlı Mimari)** prensipleriyle geliştirilmiştir. 

### Çok Katmanlı Yapı (Layered Architecture)
- **Domain/Entities Layer:** Sistemin kalbi niteliğindedir. İş nesneleri (Entities) ve temel kısıtlamalar burada izole edilmiştir.
- **Data Access Layer (DAL):** Veritabanı operasyonlarının soyutlandığı katmandır. EF Core üzerinden veri erişimi sağlanır.
- **Business Logic Layer (BLL):** Temel iş kurallarının (Business Rules), validasyonların ve servis orkestrasyonunun yürütüldüğü katmandır.
- **Presentation/API Layer:** İstemcilerle iletişimin kurulduğu, RESTful standartlarında tasarlanmış dışa açılan kapıdır.

### 🧠 Yapay Zekâ Entegrasyonu (AI-Assisted Ecosystem)
Geleneksel modüllerin yanı sıra proje, aşağıdaki alanlarda AI desteği sunar:
- **Akıllı Veri Analizi ve Öngörücü Modeller:** Satış verileri ve kullanıcı etkileşimlerinden öğrenerek dinamik stratejiler geliştirilmesi.
- **Gelişmiş Arama ve Filtreleme (Smart Search):** Doğal dil işleme yetenekleriyle zenginleştirilmiş ürün keşif deneyimi.
- **Otomatize Edilmiş İçerik Üretimi:** Ürün tanımlamaları, otomatik kod üretimi ve etiketleme süreçlerinde makine öğrenmesi destekli asistan mekanizmaları.

---

## 🛠 Teknoloji Yığını (Tech Stack)

Uygulamanın omurgası, güncel ve robust teknolojilerle oluşturulmuştur:

| Kategori | Teknoloji / Araç | Açıklama / Seçim Nedeni |
| :--- | :--- | :--- |
| **Backend** | `ASP.NET Core 8/9` | Yüksek performanslı, cross-platform ve modern API geliştirme altyapısı. |
| **ORM** | `Entity Framework Core` | Code-First yaklaşımı ile nesne-ilişkisel eşleme (Object-Relational Mapping). |
| **Database** | `PostgreSQL` | Katı veri tutarlılığı (ACID compliance) ve karmaşık sorgulardaki üstün performansı sebebiyle tercih edilmiştir. |
| **Frontend** | `Modern Web UI` | Responsive (duyarlı), kullanıcı odaklı ve akıcı bir modern arayüz tasarımı. |
| **Mimari Desenler** | `Repository`, `Unit of Work` | Veri erişiminin merkezi yönetimi ve transaction güvenliği. |
| **IoC/DI** | `Dependency Injection` | Loosely coupled (gevşek bağlı) bileşenler ve kolay test edilebilirlik. |

---

## ✨ Öne Çıkan Özellikler

- 🔐 **Kapsamlı Kullanıcı ve Yetki Yönetimi:** Güvenli ve yapılandırılabilir kimlik yönetimi.
- 📦 **Dinamik Ürün Kataloğu:** Esnek veri modeliyle yönetilebilir kompleks ürün hiyerarşisi.
- 🤖 **AI Destekli İçerik ve Filtreleme:** Kullanıcı davranışlarına ve veri paternlerine göre akıllı liste optimizasyonu.
- 📈 **Kurumsal Ölçeklenebilirlik:** Yüksek trafiği yönetebilecek dayanıklı altyapı.

---

## ⚙️ Kurulum ve Konfigürasyon (Guide)

Projeyi yerel ortamınızda (Local Environment) ayağa kaldırmak ve yayına almak (Deployment) için aşağıdaki adımları izleyiniz.

### Ön Koşullar (Prerequisites)
- [.NET 8/9 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/)
- [Git](https://git-scm.com/)

### 1. Repoyu Klonlama
```bash
git clone https://github.com/your-username/InterFullMarkt.git
cd InterFullMarkt
```

### 2. Veritabanı Konfigürasyonu
Uygulamanın başlangıç projesinde yer alan `appsettings.json` dosyasına PostgreSQL bağlantı dizesini ekleyin:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=InterFullMarktDb;Username=postgres;Password=YourSecurePassword"
}
```

### 3. EF Core Migrasyonları (Data Migrations)
Veritabanı şemasını oluşturmak ve kurumsal veritabanı altyapısını başlatmak için .NET CLI kullanarak migration adımlarını uygulayın:
```bash
# Uygulamayı derleyin ve bağımlılıkları yükleyin
dotnet restore

# Migration uygulayın (Proje adlarınızı kendi yapınıza göre düzenleyin)
dotnet ef database update --project InterFullMarkt.DataAccess --startup-project InterFullMarkt.API
```

### 4. Uygulamayı Başlatma (Deployment)
Tüm konfigürasyonlar tamamlandıktan sonra uygulamayı çalıştırın:
```bash
dotnet build
dotnet run --project InterFullMarkt.API
```
Servisler başarıyla başlatıldığında, tanımlanan port (örn: `https://localhost:5001`) üzerinden RESTful API uç noktalarına erişebilirsiniz.

---

## 👨‍💻 Geliştirici Bilgisi

**Hüseyin Orhan Kırtay**  
*Kıdemli Yazılım Mimarı & Proje Yöneticisi*

---
*Bu proje, akademik vizyon ile endüstriyel standartların kesişim noktasında, inovatif teknolojilere yön vermek amacıyla tasarlanmıştır.*
