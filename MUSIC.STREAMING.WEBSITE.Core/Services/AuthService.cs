using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;
using MUSIC.STREAMING.WEBSITE.Core.Entities;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using MUSIC.STREAMING.WEBSITE.Core.Interfaces.Repository;
using BCrypt.Net;
using System.Net;


namespace MUSIC.STREAMING.WEBSITE.Core.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    public AuthService(IConfiguration configuration, IUserRepository userRepository, IEmailService emailService)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _emailService = emailService;
    }
    public async Task<AuthResponseDto> LoginWithGoogleAsync(string idToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() { _configuration["GoogleAuth:ClientId"] }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (Exception ex)
        {
            throw new Exception("Google Token không hợp lệ: " + ex.Message);
        }
        var user = await _userRepository.GetByEmailAsync(payload.Email);
        bool isNewUser = false;
        if (user == null)
        {
            isNewUser = true;
            user = new User
            {
                UserId = Guid.NewGuid(),
                Email = payload.Email,
                Username = payload.Email,
                FullName = payload.Name,
                Avatar = payload.Picture,
                AuthSource = "google",
                ExternalId = payload.Subject,
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            await _userRepository.CreateAsync(user);
        }
        else
        {
            bool isChanged = false;
            if (user.AuthSource == "local")
            {
                user.AuthSource = "google";
                user.ExternalId = payload.Subject;
                isChanged = true;
            }
            if (string.IsNullOrEmpty(user.Avatar))
            {
                user.Avatar = payload.Picture;
                isChanged = true;
            }
            if (isChanged)
            {
                await _userRepository.UpdateAsync(user.UserId, user);
            }
        }
        var token = GenerateJwtToken(user);
        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Avatar = user.Avatar,
            Role = user.Role,
            IsNewUser = isNewUser
        };
    }

    public async Task<string> SetUserRoleAsync(Guid userId, string newRole)
    {
        if (newRole != "User" && newRole != "Artist")
        {
            throw new ArgumentException("Vai trò không hợp lệ.");
        }
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy người dùng.");
        }
        user.Role = newRole;
        user.UpdatedAt = DateTime.Now;
        await _userRepository.UpdateAsync(user.UserId, user);

        var newToken = GenerateJwtToken(user);

        return newToken; // Trả về chuỗi Token
    }

    // Hàm private để sinh Token
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("UserId", user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"])),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Đăng ký
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Check trùng Email
        var existEmail = await _userRepository.GetByEmailAsync(dto.Email);
        if (existEmail != null) throw new Exception("Email này đã được sử dụng.");

        // Check trùng Username
        var existUser = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existUser != null) throw new Exception("Tên đăng nhập này đã tồn tại.");

        // Mã hóa mật khẩu
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Xử lý avatar động
        string finalAvatarUrl = dto.Avatar;


        if (string.IsNullOrEmpty(finalAvatarUrl))
        {
            // Lấy tên để hiển thị 
            string nameForAvatar = !string.IsNullOrEmpty(dto.FullName) ? dto.FullName : dto.Username;

            // Mã hóa tên để đưa vào URL 
            string encodedName = WebUtility.UrlEncode(nameForAvatar);

            // Tạo link từ UI Avatars
            // background=random: Tự chọn màu nền ngẫu nhiên cho mỗi user
            // color=fff: Chữ màu trắng
            // size=128: Kích thước ảnh
            // bold=true: Chữ in đậm
            finalAvatarUrl = $"https://ui-avatars.com/api/?name={encodedName}&background=random&color=fff&size=128&bold=true";
        }

        // Tạo User entity
        var newUser = new User
        {
            UserId = Guid.NewGuid(),
            Email = dto.Email,
            Username = dto.Username,
            Password = passwordHash, // Lưu pass đã mã hóa
            FullName = dto.FullName ?? dto.Username,
            Avatar = finalAvatarUrl,
            Role = "User",
            AuthSource = "local", // Đánh dấu là đăng ký thường
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Lưu vào DB
        await _userRepository.CreateAsync(newUser);

        if (dto.FavoriteGenreIds != null && dto.FavoriteGenreIds.Any())
        {
            await _userRepository.AddUserFavoriteGenresAsync(newUser.UserId, dto.FavoriteGenreIds);
        }

        // Tạo Token trả về luôn 
        var token = GenerateJwtToken(newUser);

        return new AuthResponseDto
        {
            Token = token,
            FullName = newUser.FullName,
            Role = newUser.Role,
            IsNewUser = true
        };
    }

    // Đăng nhập
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // 1. Tìm user theo Username HOẶC Email
        var user = await _userRepository.GetByUsernameOrEmailAsync(dto.Identifier);

        if (user == null)
        {
            throw new Exception("Tài khoản hoặc mật khẩu không chính xác.");
        }

        // 2. Nếu user đăng nhập bằng Google mà không có password -> Chặn
        if (string.IsNullOrEmpty(user.Password))
        {
            throw new Exception("Tài khoản này đăng ký bằng Google. Vui lòng chọn 'Đăng nhập bằng Google'.");
        }

        // 3. Kiểm tra mật khẩu 
        bool isValidPass = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

        if (!isValidPass)
        {
            throw new Exception("Tài khoản hoặc mật khẩu không chính xác.");
        }

        // 4. Thành công -> Tạo token
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Avatar = user.Avatar,
            Role = user.Role,
            IsNewUser = false
        };
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        // Validate cơ bản
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new Exception("Mật khẩu xác nhận không khớp.");
        }

        // Lấy User từ DB
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("Người dùng không tồn tại.");

        // Nếu là tài khoản Google -> Không có mật khẩu để đổi
        if (string.IsNullOrEmpty(user.Password))
        {
            throw new Exception("Tài khoản Google không thể đổi mật khẩu tại đây.");
        }

        // Kiểm tra mật khẩu cũ 
        bool isCorrect = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password);
        if (!isCorrect)
        {
            throw new Exception("Mật khẩu hiện tại không chính xác.");
        }

        // Hash mật khẩu mới
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        // Cập nhật DB
        user.Password = newPasswordHash;
        user.UpdatedAt = DateTime.Now;
        await _userRepository.UpdateAsync(user.UserId, user);

        // Gửi Email thông báo
        try
        {
            string emailBody = $@"
                <h3>Xin chào {user.FullName},</h3>
                <p>Mật khẩu tài khoản Melonia của bạn vừa được thay đổi thành công.</p>
                <p>Thời gian: {DateTime.Now}</p>
                <p>Nếu không phải bạn thực hiện, vui lòng liên hệ admin ngay lập tức.</p>";

            await _emailService.SendEmailAsync(user.Email, "Thông báo thay đổi mật khẩu", emailBody);
        }
        catch
        {
        }
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return;
        }

        if (user.AuthSource == "google") throw new Exception("Tài khoản Google không thể reset mật khẩu.");

        // Tạo Token ngẫu nhiên (UUID)
        string token = Guid.NewGuid().ToString();

        // Lưu token vào DB (Hết hạn sau 15 phút)
        user.ResetToken = token;
        user.ResetTokenExpiry = DateTime.Now.AddMinutes(15);
        await _userRepository.UpdateAsync(user.UserId, user);

        string resetLink = $"http://localhost:3000/reset-password?token={token}";

        string emailBody = $@"
        <h3>Yêu cầu đặt lại mật khẩu</h3>
        <p>Bấm vào link dưới đây để đặt lại mật khẩu (Hết hạn sau 15 phút):</p>
        <a href='{resetLink}'>Bấm vào đây để đổi mật khẩu</a>";

        await _emailService.SendEmailAsync(user.Email, "Quên mật khẩu Music App", emailBody);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword) throw new Exception("Mật khẩu xác nhận không khớp.");

        var user = await _userRepository.GetByResetTokenAsync(dto.Token);

        if (user == null || user.ResetTokenExpiry < DateTime.Now)
        {
            throw new Exception("Đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        // Đổi mật khẩu
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        // Xóa Token để không dùng lại được nữa
        user.ResetToken = null;
        user.ResetTokenExpiry = null;
        user.UpdatedAt = DateTime.Now;

        await _userRepository.UpdateAsync(user.UserId, user);
    }
}
