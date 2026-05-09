# 🚀 MonoBase - Modern NoSQL & SQLite Data Engine

MonoBase, verilerinizi esnek (NoSQL) bir yapıda saklamanızı sağlayan, SQLite tabanlı, hafif ve yüksek performanslı bir veri yönetim servisidir. Geleneksel ilişkisel veritabanlarının karmaşıklığından kurtulup, verilerinizi JSON formatında, güvenli ve hızlı bir şekilde yönetmeniz için tasarlanmıştır.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/download)
[![Database: SQLite](https://img.shields.io/badge/Database-SQLite-003B57.svg)](https://www.sqlite.org/)

---

## 💡 MonoBase Nedir?

MonoBase, "Hizmet Olarak Veritabanı" (DBaaS) mantığını yerel veya özel sunucularınıza taşır. Her kullanıcıya özel izole edilmiş veritabanı yolları oluşturur ve verileri **Collection (Koleksiyon)** mantığıyla saklar. Tablo tasarımı yapmanıza gerek kalmadan, JSON objelerinizi doğrudan API üzerinden gönderip saklayabilirsiniz.

### ✨ Temel Özellikler

* **NoSQL Esnekliği:** Şema (Schema) tasarımı gerektirmez. JSON formatındaki her türlü veriyi saklayabilir.
* **API Key Güvenliği:** Her kullanıcı için benzersiz API anahtarları ile yetkilendirme sağlar.
* **İzole Veri Alanları:** Her kullanıcının verisi kendi fiziksel SQLite dosyasında (`.db`) güvenle saklanır.
* **Full CRUD Desteği:** Veri ekleme (POST), okuma (GET), güncelleme (PUT) ve silme (DELETE) işlemleri için hazır uç noktalar.
* **Performanslı & Hafif:** SQLite'ın gücünü arkasına alarak düşük kaynak tüketimi ve yüksek hız sunar.

---

## 🛠️ Mimari Yapı

MonoBase iki temel bileşenden oluşur:
1.  **MonoBase API:** Veritabanı işlemlerini yöneten merkezi motor.
2.  **Client Application:** API'yi tüketen (Örn: Not Defteri uygulaması) son kullanıcı arayüzü.

---

## 🚀 Hızlı Başlangıç

### 1. Kurulum
Projeyi klonlayın ve gerekli bağımlılıkları yükleyin:

```bash
git clone [https://github.com/kullaniciadi/monobase.git](https://github.com/kullaniciadi/monobase.git)
cd monobase
dotnet restore