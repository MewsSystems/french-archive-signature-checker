using System.Security.Cryptography;

namespace Mews.Fiscalization.SignatureChecker;

public static class CryptoServiceProvider
{
    private const string ProductionKey = "<RSAKeyValue><Modulus>1NWh/S4jzrtO/N3Dm1gkfaok0A/u04/pExwDES2SmubDSeFwssXGBqWZ4UsIONKXdXkDrJ1kqgednDjkdkCYW6BmYr/Ds1U+3viiZtl6nBaJp2MTGLSGDR/9algLOYr60bk/18KJFbr2xzKMadimrQ5J2p0LVkfPvIcX0d69xbei0Kzn5bFOKlA3WgimOdh9ed9ZZ7IEylVVQQG6dWElWbbMgDiOzwyGiPn7/9Tb+oh6zxBiazc5/HpACC/RBpr85aIJQtXgDTK4mlwjrL/WHoAHyehwQSJ08QsRyd/tux8QxBZDk4hWoipC8W+tlyFg4n/n79AuxQX0TaOmMQFjm4hVVX3cS34zmELpGsZiH2uMtGJqgehchlF+FSE/12v2p9sEesPLANEmajjChmajQgvbnlS/SSPc/gd2FYb0pq8jLM/90idOKKMx7ZbxS/8yro8VbS79VR8UdMKRzJLFDbHeDa0hVVbaSWR8jodnp7gPd55SIZ47AcDrpKNQ4s9R1Ah38Czq/QKInCkNjTCC+JdxrF+XnqsUFL5DbOUDbxDzVq0TJj36xMC/LICcIbTmt7rg0auqXq4RhG6lnwyCpUaUr5YZ4pJm44kA94Zgy+o/7wpqYhYayGIZqu1VgK82gBs5m5uPXHiTFfb4cZzCZG356I8nzr2LQjTe4hXDaRE=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
    private const string DevelopKey = "<RSAKeyValue><Modulus>ufmEzn3l/SsD01uvqILxYTEy8XV47r/uMD4sOZ07J4P0kx73LBcbMx+Gag4T9nYzTs0KmBPl6ogSdqA1hsJQ0BBlKFVZK53WTe1GjtXVPyhyuvfTd6pXPuW7ozBcd0xuHU5qhBY4TEjrbRZoSMHLMDsQB4Egdx+H5Y4T9TXSri7yraxUUqNNET5zU1mpxJqC3R6M6UcBiMagNYvcrL/iWLLvfhC3pjFmBiS/0rxAU5TRiWumwlFrkUpt7/dUMBDD1Adt1/nPEA5IZBscoFQWQGi1tCpzs1K71nJF04BgrALpetf3KYucrbiY85ANIAjE8pvDaYQVOSg5xIRIUVkd92uA8dFc2QRd6EL7wxZ6u3y5FsTm9ILaEfc54MlSVWx2E+bTvBL2jK1Pz+hSNBOWnRJbAJFWuToCGOykkyHOqQwmPX5MhbJ5q3qYh//E8D2SeJqc1Nr3NDaUNhJf1gkkr53yN+kbF9M2+xqn8p1jV6BUjQI+qDYeTyuBVgxEz5+H0xEyrSZ5DDjm9HsxXhzdXhHK7HDtvrSjgAr4HGmSODCuWpynzjGdNVEANsfpjT//zhgGEkW0JHiRmXjRbNslmspeW7xG5/uOxlZIFVahM0n544hoQbZx9mYcNMDkuT6d9wgjFEKAXVzn+45gPZ627eeVCF3pbAJCSx5EVPJfZGU=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    public static RSACryptoServiceProvider GetProduction()
    {
        return GetProvider(ProductionKey);
    }

    public static RSACryptoServiceProvider GetDevelop()
    {
        return GetProvider(DevelopKey);
    }

    private static RSACryptoServiceProvider GetProvider(string xmlKey)
    {
        var rsa = new RSACryptoServiceProvider();
        rsa.FromXmlString(xmlKey);
        return rsa;
    }
}