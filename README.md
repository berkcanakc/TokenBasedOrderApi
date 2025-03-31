Yalnızca token yönetimiyle ilgili bir proje olduğu için statik veriler kullandım. Minimal API mantığıyla ilerledim. Sorudaki case pek açık değildi. 1 saatlik token alıyoruz apiden. 
Sonrasında postman üzerinden authorization tipini bearer seçerek tokenimizle birlikte orders endpointine istek atıyoruz.
bir saat içerisinde tokenin 5 kullanım hakkı bulunuyor. Token süresi bitmeden kullanım hakkı bitse dahi bize yeni token vermiyor böylelikle bir saat içerisinde yalnızca 5 istek yollama hakkımız oluyor.
Dilersek de token-status endpointinde tokenimizin kaç kere kullanıldığını ve ne zaman expire olacağını görüntüleyebiliyoruz
