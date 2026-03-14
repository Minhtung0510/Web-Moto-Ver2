using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MotoBikeStore.Models;

namespace MotoBikeStore.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // ── Gửi mail đặt hàng thành công (gọi từ OrdersController) ────────────
        public async Task SendOrderPlacedAsync(Order order)
        {
            var subject = $"[MotoBike Store] Xác nhận đơn hàng #{order.OrderCode}";
            var body    = BuildOrderPlacedHtml(order);
            await SendAsync(order.Email ?? "", order.CustomerName, subject, body);
        }

        // ── Gửi mail khi admin xác nhận đơn (gọi từ AdminController) ──────────
        public async Task SendOrderConfirmedAsync(Order order)
        {
            var subject = $"[MotoBike Store] Đơn hàng #{order.OrderCode} đã được xác nhận";
            var body    = BuildOrderConfirmedHtml(order);
            await SendAsync(order.Email ?? "", order.CustomerName, subject, body);
        }

        // ── Gửi mail khi đơn đang giao ────────────────────────────────────────
        public async Task SendOrderShippingAsync(Order order)
        {
            var subject = $"[MotoBike Store] Đơn hàng #{order.OrderCode} đang được giao";
            var body    = BuildOrderShippingHtml(order);
            await SendAsync(order.Email ?? "", order.CustomerName, subject, body);
        }

        // ── Core: gửi mail qua SMTP ────────────────────────────────────────────
        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            // Bỏ qua nếu không có email người nhận
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:Username"] ?? "";
            var smtpPass = _config["Email:Password"] ?? "";
            var fromName = _config["Email:FromName"] ?? "MotoBike Store";

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, smtpUser));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"[EMAIL] Sent '{subject}' to {toEmail}");
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không crash app
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            }
        }

        // ── HTML Templates ─────────────────────────────────────────────────────
        private static string BuildOrderPlacedHtml(Order order)
        {
            var itemRows = "";
            if (order.Details != null)
            {
                foreach (var d in order.Details)
                {
                    var name = d.Product?.Name ?? $"Sản phẩm #{d.ProductId}";
                    itemRows += $@"
                    <tr>
                        <td style='padding:10px;border-bottom:1px solid #f0f0f0'>{name}</td>
                        <td style='padding:10px;border-bottom:1px solid #f0f0f0;text-align:center'>{d.Quantity}</td>
                        <td style='padding:10px;border-bottom:1px solid #f0f0f0;text-align:right'>{d.UnitPrice:N0}₫</td>
                    </tr>";
                }
            }

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f5f5f5'>
  <div style='max-width:600px;margin:30px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08)'>

    <!-- Header -->
    <div style='background:linear-gradient(135deg,#667eea,#764ba2);padding:30px;text-align:center'>
      <h1 style='color:#fff;margin:0;font-size:24px'>🏍️ MotoBike Store</h1>
      <p style='color:rgba(255,255,255,0.85);margin:8px 0 0'>Cảm ơn bạn đã đặt hàng!</p>
    </div>

    <!-- Body -->
    <div style='padding:30px'>
      <h2 style='color:#333;margin-top:0'>Xin chào {order.CustomerName}!</h2>
      <p style='color:#666'>Đơn hàng của bạn đã được ghi nhận thành công. Chúng tôi sẽ liên hệ xác nhận sớm nhất.</p>

      <!-- Order Info -->
      <div style='background:#f8f9ff;border-radius:8px;padding:16px;margin:20px 0'>
        <table style='width:100%;border-collapse:collapse'>
          <tr>
            <td style='color:#999;padding:4px 0'>Mã đơn hàng</td>
            <td style='color:#333;font-weight:bold;text-align:right'>#{order.OrderCode}</td>
          </tr>
          <tr>
            <td style='color:#999;padding:4px 0'>Ngày đặt</td>
            <td style='color:#333;text-align:right'>{order.OrderDate:dd/MM/yyyy HH:mm}</td>
          </tr>
          <tr>
            <td style='color:#999;padding:4px 0'>Địa chỉ giao hàng</td>
            <td style='color:#333;text-align:right'>{order.Address}</td>
          </tr>
          <tr>
            <td style='color:#999;padding:4px 0'>Hình thức thanh toán</td>
            <td style='color:#333;text-align:right'>{order.PaymentMethod}</td>
          </tr>
        </table>
      </div>

      <!-- Products -->
      <table style='width:100%;border-collapse:collapse;margin:20px 0'>
        <thead>
          <tr style='background:#f0f0f0'>
            <th style='padding:10px;text-align:left;font-size:13px'>Sản phẩm</th>
            <th style='padding:10px;text-align:center;font-size:13px'>SL</th>
            <th style='padding:10px;text-align:right;font-size:13px'>Đơn giá</th>
          </tr>
        </thead>
        <tbody>{itemRows}</tbody>
      </table>

      <!-- Total -->
      <div style='border-top:2px solid #667eea;padding-top:16px'>
        <table style='width:100%'>
          <tr>
            <td style='color:#666'>Tạm tính</td>
            <td style='text-align:right;color:#333'>{order.Subtotal:N0}₫</td>
          </tr>
          <tr>
            <td style='color:#666'>Phí vận chuyển</td>
            <td style='text-align:right;color:#333'>{order.ShippingFee:N0}₫</td>
          </tr>
          {(order.DiscountAmount > 0 ? $"<tr><td style='color:#28a745'>Giảm giá</td><td style='text-align:right;color:#28a745'>-{order.DiscountAmount:N0}₫</td></tr>" : "")}
          <tr>
            <td style='font-weight:bold;font-size:16px;padding-top:8px'>Tổng cộng</td>
            <td style='text-align:right;font-weight:bold;font-size:18px;color:#e53935;padding-top:8px'>{order.Total:N0}₫</td>
          </tr>
        </table>
      </div>

      <!-- Track button -->
      <div style='text-align:center;margin:30px 0'>
        <a href='http://localhost:5000/Orders/Track?id={order.OrderCode}'
           style='background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px'>
          🚚 Theo dõi đơn hàng
        </a>
      </div>
    </div>

    <!-- Footer -->
    <div style='background:#f8f8f8;padding:20px;text-align:center;border-top:1px solid #eee'>
      <p style='color:#999;font-size:13px;margin:0'>Cần hỗ trợ? Liên hệ: <a href='mailto:support@motobike.vn' style='color:#667eea'>support@motobike.vn</a></p>
      <p style='color:#bbb;font-size:12px;margin:8px 0 0'>© 2024 MotoBike Store</p>
    </div>
  </div>
</body>
</html>";
        }

        private static string BuildOrderConfirmedHtml(Order order)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f5f5f5'>
  <div style='max-width:600px;margin:30px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08)'>

    <div style='background:linear-gradient(135deg,#11998e,#38ef7d);padding:30px;text-align:center'>
      <div style='font-size:48px'>✅</div>
      <h1 style='color:#fff;margin:8px 0 0;font-size:22px'>Đơn hàng đã được xác nhận!</h1>
    </div>

    <div style='padding:30px'>
      <h2 style='color:#333;margin-top:0'>Xin chào {order.CustomerName}!</h2>
      <p style='color:#666;font-size:15px;line-height:1.6'>
        Đơn hàng <strong style='color:#333'>#{order.OrderCode}</strong> của bạn đã được xác nhận.
        Chúng tôi đang chuẩn bị hàng và sẽ giao đến bạn sớm nhất có thể.
      </p>

      <div style='background:#f0fff4;border:1px solid #b7f5c8;border-radius:8px;padding:16px;margin:20px 0'>
        <table style='width:100%;border-collapse:collapse'>
          <tr>
            <td style='color:#555;padding:4px 0'>Mã đơn hàng</td>
            <td style='color:#333;font-weight:bold;text-align:right'>#{order.OrderCode}</td>
          </tr>
          <tr>
            <td style='color:#555;padding:4px 0'>Tổng tiền</td>
            <td style='color:#e53935;font-weight:bold;text-align:right'>{order.Total:N0}₫</td>
          </tr>
          <tr>
            <td style='color:#555;padding:4px 0'>Địa chỉ giao hàng</td>
            <td style='color:#333;text-align:right'>{order.Address}</td>
          </tr>
          {(!string.IsNullOrEmpty(order.TrackingNumber) ? $"<tr><td style='color:#555;padding:4px 0'>Mã vận đơn</td><td style='color:#333;font-weight:bold;text-align:right'>{order.TrackingNumber}</td></tr>" : "")}
        </table>
      </div>

      <div style='text-align:center;margin:30px 0'>
        <a href='http://localhost:5000/Orders/Track?id={order.OrderCode}'
           style='background:linear-gradient(135deg,#11998e,#38ef7d);color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px'>
          🚚 Theo dõi đơn hàng
        </a>
      </div>
    </div>

    <div style='background:#f8f8f8;padding:20px;text-align:center;border-top:1px solid #eee'>
      <p style='color:#999;font-size:13px;margin:0'>© 2024 MotoBike Store | <a href='mailto:support@motobike.vn' style='color:#667eea'>support@motobike.vn</a></p>
    </div>
  </div>
</body>
</html>";
        }

        private static string BuildOrderShippingHtml(Order order)
        {
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f5f5f5'>
  <div style='max-width:600px;margin:30px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08)'>

    <div style='background:linear-gradient(135deg,#4facfe,#00f2fe);padding:30px;text-align:center'>
      <div style='font-size:48px'>🚚</div>
      <h1 style='color:#fff;margin:8px 0 0;font-size:22px'>Đơn hàng đang được giao!</h1>
    </div>

    <div style='padding:30px'>
      <h2 style='color:#333;margin-top:0'>Xin chào {order.CustomerName}!</h2>
      <p style='color:#666;font-size:15px;line-height:1.6'>
        Đơn hàng <strong style='color:#333'>#{order.OrderCode}</strong> đang trên đường đến tay bạn.
        Vui lòng chú ý điện thoại để nhận hàng.
      </p>

      <div style='background:#e8f4ff;border:1px solid #b3d9ff;border-radius:8px;padding:16px;margin:20px 0'>
        <table style='width:100%;border-collapse:collapse'>
          <tr>
            <td style='color:#555;padding:4px 0'>Mã đơn hàng</td>
            <td style='color:#333;font-weight:bold;text-align:right'>#{order.OrderCode}</td>
          </tr>
          {(!string.IsNullOrEmpty(order.TrackingNumber) ? $"<tr><td style='color:#555;padding:4px 0'>Mã vận đơn</td><td style='color:#0d6efd;font-weight:bold;text-align:right'>{order.TrackingNumber}</td></tr>" : "")}
          <tr>
            <td style='color:#555;padding:4px 0'>Địa chỉ nhận hàng</td>
            <td style='color:#333;text-align:right'>{order.Address}</td>
          </tr>
          <tr>
            <td style='color:#555;padding:4px 0'>Tổng tiền COD</td>
            <td style='color:#e53935;font-weight:bold;text-align:right'>{order.Total:N0}₫</td>
          </tr>
        </table>
      </div>

      <div style='text-align:center;margin:30px 0'>
        <a href='http://localhost:5000/Orders/Track?id={order.OrderCode}'
           style='background:linear-gradient(135deg,#4facfe,#00f2fe);color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px'>
          📦 Xem trạng thái đơn hàng
        </a>
      </div>
    </div>

    <div style='background:#f8f8f8;padding:20px;text-align:center;border-top:1px solid #eee'>
      <p style='color:#999;font-size:13px;margin:0'>© 2024 MotoBike Store | <a href='mailto:support@motobike.vn' style='color:#667eea'>support@motobike.vn</a></p>
    </div>
  </div>
</body>
</html>";
        }
    }
}
