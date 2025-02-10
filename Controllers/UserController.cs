using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using API.Data;
using Microsoft.AspNetCore.Identity;


namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly PasswordHasher<User> _passwordHasher;
        public UserController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
{
    _context = context;
    _httpClient = httpClientFactory.CreateClient();
    _passwordHasher = new PasswordHasher<User>();
}
        [HttpGet("list")]
public IActionResult ListUsers(string username = null, int? birthYear = null)
{
    var usersQuery = _context.User.AsQueryable();   
    // Kullanıcı adına göre filtreleme
    if (!string.IsNullOrEmpty(username))
    {
        usersQuery = usersQuery.Where(u => u.Username.Equals(username));
    }   
    // Doğum yılına göre filtreleme
    if (birthYear.HasValue)
    {
        usersQuery = usersQuery.Where(u => u.BirthYear == birthYear.Value);
    }
        var filteredUsers = usersQuery
        .Select(user => new
        {
            user.Username,
            user.Ad,
            user.Soyad,
            user.TcKimlikNo,
            user.BirthYear
        })
        .ToList();
    
    if (filteredUsers.Count == 0)
    {
        return Ok(new { message = "Kullanıcı bulunamadı." });
    }
    return Ok(filteredUsers);
}
        [HttpPost("register")]
public async Task<IActionResult> Register([FromBody] User user)  //
{
    if (user == null)
    {
    return BadRequest("Kullanıcı bilgileri geçersiz.");   
    }
            //Kimlik no doğrulaması
    bool isValid = await ValidateTCKimlikNo(user.TcKimlikNo, user.Ad, user.Soyad, user.BirthYear);
    if (!isValid)
    {
        return BadRequest("T.C. Kimlik No doğrulaması başarısız.");
    }       //Password hashleyip kullanıcıyı kayıt etme
    user.HashPassword = _passwordHasher.HashPassword(user, user.HashPassword);
    _context.User.Add(user);
    await _context.SaveChangesAsync();
        return Ok("Kullanıcı başarıyla kaydedildi.");
    }
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel loginUser) 
    {
     if (loginUser == null)
     {
         return BadRequest("Geçersiz giriş bilgileri.");
     }
     var user = _context.User
         .FirstOrDefault(u => u.Username == loginUser.Username);   
        if(user==null){
            return BadRequest("Geçersiz giriş bilgileri");
        }
       
    var passwordVerificationResult = _passwordHasher.VerifyHashedPassword
        (user, user.HashPassword, loginUser.Password);
    if (passwordVerificationResult == PasswordVerificationResult.Failed) {
            return Unauthorized("Kullanıcı adı veya şifre hatalı.");
    }
            return Ok("Giriş başarılı.");
    }
        //kimlik no doğrulama fonksiyonu
    private async Task<bool> ValidateTCKimlikNo(string tcKimlikNo, string ad, string soyad, int birthYear)
    {
        string soapRequest =
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                           xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                           xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <TCKimlikNoDogrula xmlns=""http://tckimlik.nvi.gov.tr/WS"">
                        <TCKimlikNo>{tcKimlikNo}</TCKimlikNo>
                        <Ad>{ad.ToUpper()}</Ad>
                        <Soyad>{soyad.ToUpper()}</Soyad>
                        <DogumYili>{birthYear}</DogumYili>
                    </TCKimlikNoDogrula>
                </soap:Body>
            </soap:Envelope>";
        var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
        HttpResponseMessage response = await _httpClient.PostAsync("https://tckimlik.nvi.gov.tr/Service/KPSPublic.asmx", content);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        string responseString = await response.Content.ReadAsStringAsync();
        XDocument xmlResponse = XDocument.Parse(responseString);
      
        var result = xmlResponse.Descendants().FirstOrDefault(x => x.Name.LocalName == "TCKimlikNoDogrulaResult")?.Value;
        return result == "true";
    }
}
}
