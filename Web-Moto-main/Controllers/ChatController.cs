using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using System.Text;
using System.Text.Json;

namespace MotoBikeStore.Controllers
{
    public class ChatController : Controller
    {
        private readonly MotoBikeContext _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public ChatController(MotoBikeContext db, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _db                = db;
            _config            = config;
            _httpClientFactory = httpClientFactory;
        }

        // POST /Chat/Send
        [HttpPost]
        public async Task<JsonResult> Send([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return Json(new { reply = "Vui lòng nhập câu hỏi." });

            // ── Build system prompt với context sản phẩm từ DB ────────────────
            var products   = _db.Products.ToList();
            var categories = _db.Categories.ToList();

            var catalogText = new StringBuilder();
            catalogText.AppendLine("DANH SÁCH SẢN PHẨM HIỆN CÓ:");
            catalogText.AppendLine("---");

            foreach (var cat in categories)
            {
                var catProducts = products.Where(p => p.CategoryId == cat.Id).ToList();
                if (!catProducts.Any()) continue;

                catalogText.AppendLine($"\n[{cat.Name.ToUpper()}]");
                foreach (var p in catProducts)
                {
                    catalogText.AppendLine(
                        $"- {p.Name} | Hãng: {p.Brand} | Giá: {p.Price:N0}₫" +
                        (p.OldPrice.HasValue ? $" (giá gốc {p.OldPrice:N0}₫)" : "") +
                        $" | Động cơ: {p.Engine} | Bình xăng: {p.Fuel}" +
                        (p.Stock > 0 ? $" | Còn hàng: {p.Stock}" : " | HẾT HÀNG") +
                        (string.IsNullOrEmpty(p.Description) ? "" : $" | {p.Description}")
                    );
                }
            }

            var systemPrompt = $@"Bạn là trợ lý tư vấn của MotoBike Store - cửa hàng xe máy và phụ tùng tại Việt Nam.
Nhiệm vụ của bạn là giúp khách hàng chọn xe và phụ tùng phù hợp với nhu cầu và ngân sách.

{catalogText}

NGUYÊN TẮC TƯ VẤN:
- Chỉ tư vấn dựa trên sản phẩm có trong danh sách trên
- Hỏi thêm nhu cầu nếu cần (đi làm/đường dài/đua xe, ngân sách, ưu tiên tiết kiệm hay mạnh mẽ)
- Trả lời ngắn gọn, thân thiện, dùng tiếng Việt
- Nếu sản phẩm hết hàng thì thông báo và gợi ý thay thế
- Không bịa ra sản phẩm không có trong danh sách
- Có thể gợi ý thêm phụ tùng phù hợp với xe khách chọn
- Kết thúc bằng cách mời khách thêm vào giỏ hàng nếu đã chọn được xe";

            // ── Lấy OpenAI API key ────────────────────────────────────────────
            var apiKey = _config["OpenAI:ApiKey"] ?? "";
            if (string.IsNullOrWhiteSpace(apiKey))
                return Json(new { reply = "Chatbot chưa được cấu hình. Vui lòng liên hệ admin." });

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Build messages: system + history + user message mới
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Thêm history (tối đa 10 tin gần nhất)
                var history = (request.History ?? new List<ChatMessage>()).TakeLast(10);
                foreach (var h in history)
                    messages.Add(new { role = h.Role, content = h.Content });

                // Thêm tin nhắn hiện tại
                messages.Add(new { role = "user", content = request.Message });

                var body = new
                {
                    model       = _config["OpenAI:Model"] ?? "gpt-4o-mini", // rẻ + nhanh
                    max_tokens  = 1024,
                    temperature = 0.7,
                    messages    = messages
                };

                var json     = JsonSerializer.Serialize(body);
                var content  = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CHAT ERROR] {response.StatusCode}: {err}");
                    return Json(new { reply = "Xin lỗi, tôi đang gặp sự cố. Vui lòng thử lại." });
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc    = JsonDocument.Parse(responseJson);
                var reply        = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Xin lỗi, không có phản hồi.";

                return Json(new { reply });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHAT EXCEPTION] {ex.Message}");
                return Json(new { reply = "Xin lỗi, tôi đang gặp sự cố kỹ thuật. Vui lòng thử lại sau." });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public List<ChatMessage>? History { get; set; }
    }

    public class ChatMessage
    {
        public string Role    { get; set; } = "";
        public string Content { get; set; } = "";
    }
}