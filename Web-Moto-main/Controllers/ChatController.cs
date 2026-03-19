using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            // ── Build product catalog từ DB ────────────────────────────────
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

            // ── Phân tích intent từ lịch sử + tin nhắn hiện tại ───────────
            var fullConversation = string.Join(" ",
                (request.History ?? new List<ChatMessage>())
                    .Select(h => h.Content)
                    .Append(request.Message)
            ).ToLower();

            bool isSportIntent = ContainsSportKeywords(fullConversation);

            // ── System Prompt ─────────────────────────────────────────────
            var systemPrompt = $@"Bạn là chuyên gia tư vấn xe máy tại MotoBike Store — nhiệt huyết, am hiểu sâu, và luôn đặt sự an toàn của khách hàng lên hàng đầu.

══════════════════════════════════════
QUY TRÌNH TƯ VẤN 3 BƯỚC
══════════════════════════════════════
1. KHÁM PHÁ: Nếu khách chưa rõ nhu cầu, hỏi về:
   - Mục đích sử dụng: đi làm / phượt / thể thao / đa dụng
   - Ngân sách: dưới 40tr / 40-70tr / trên 70tr
   - Kinh nghiệm lái: mới tập / có kinh nghiệm / lái lâu năm

2. PHÂN TÍCH: Đối chiếu nhu cầu với danh sách sản phẩm.

3. GỢI Ý: Đề xuất 2-3 xe phù hợp nhất kèm lý do cụ thể.

══════════════════════════════════════
{(isSportIntent ? @"⚠️  TÌNH HUỐNG ĐẶC BIỆT: KHÁCH HÀNG ĐAM MÊ TỐC ĐỘ / THỂ THAO / MẠO HIỂM
══════════════════════════════════════
Khi phát hiện khách quan tâm đến xe thể thao, tốc độ cao, hoặc hoạt động mạo hiểm:

A) TƯ VẤN AN TOÀN BẮT BUỘC (đặt trước khi giới thiệu xe):
   - Nhắc nhở về nguy cơ tai nạn khi lái xe tốc độ cao
   - Đề xuất bộ đồ bảo hộ đầy đủ: mũ full-face, giáp tay/vai/lưng, găng tay, giày riding
   - Khuyến khích tham gia khóa học lái xe thể thao nếu chưa có kinh nghiệm
   - Nhấn mạnh: 'Cảm giác mạnh thực sự đến từ sự kiểm soát, không phải từ tốc độ bừa bãi'

B) SAU KHI TƯ VẤN AN TOÀN → mới giới thiệu xe phù hợp:
   - Ưu tiên xe có hệ thống an toàn tốt: ABS, traction control nếu có
   - Nếu khách là người mới → gợi ý xe phân khối vừa (150-300cc) trước
   - Nếu khách đã có kinh nghiệm → mới giới thiệu xe mạnh hơn
   - Luôn đề cập tính năng an toàn nổi bật của từng xe được gợi ý

C) GIỌNG ĐIỆU: Đồng cảm với đam mê của khách, KHÔNG phán xét — nhưng kiên định về an toàn.
   Ví dụ mở đầu: 'Tôi hiểu cảm giác khi ngồi trên một chiếc xe mạnh thật đặc biệt! Nhưng để tận hưởng trọn vẹn, hãy để tôi chia sẻ một vài điều quan trọng trước...'
" : @"TÌNH HUỐNG THÔNG THƯỜNG
══════════════════════════════════════
Tư vấn xe phù hợp với nhu cầu và ngân sách của khách.
Nếu khách lo về giá → nhắc đến xe đang giảm giá hoặc chương trình trả góp.
")}

══════════════════════════════════════
{catalogText}

══════════════════════════════════════
NGUYÊN TẮC CHUNG
══════════════════════════════════════
- Mỗi tin nhắn ngắn gọn, có trọng tâm. Không quá 200 từ.
- Luôn kết thúc bằng 1 câu hỏi gợi mở hoặc lời kêu gọi hành động.
- Dùng emoji hợp lý để tạo cảm giác thân thiện.
- Ngôn ngữ: Tiếng Việt tự nhiên, gần gũi.
- KHÔNG bịa thêm sản phẩm ngoài danh sách.";

            // ── Lấy Gemini API key ────────────────────────────────────────
            var apiKey = _config["Gemini:ApiKey"] ?? "";
            if (string.IsNullOrWhiteSpace(apiKey))
                return Json(new { reply = "Chatbot chưa được cấu hình. Vui lòng liên hệ admin." });

            var model = _config["Gemini:Model"] ?? "gemini-2.0-flash";

            try
            {
                var client = _httpClientFactory.CreateClient();
                // Gemini dùng API key qua query param, KHÔNG dùng Authorization header

                // ── Build contents (history + tin nhắn mới) ───────────────
                var history  = (request.History ?? new List<ChatMessage>()).TakeLast(10).ToList();
                var contents = new List<GeminiContent>();

                foreach (var h in history)
                {
                    contents.Add(new GeminiContent
                    {
                        role  = h.Role == "assistant" ? "model" : "user",
                        parts = new List<GeminiPart> { new GeminiPart { text = h.Content } }
                    });
                }

                // Thêm tin nhắn mới nhất
                contents.Add(new GeminiContent
                {
                    role  = "user",
                    parts = new List<GeminiPart> { new GeminiPart { text = request.Message } }
                });

                // ── Build request body ─────────────────────────────────────
                var body = new GeminiRequest
                {
                    system_instruction = new GeminiSystemInstruction
                    {
                        parts = new List<GeminiPart> { new GeminiPart { text = systemPrompt } }
                    },
                    contents         = contents,
                    generationConfig = new GeminiGenerationConfig
                    {
                        maxOutputTokens = 2048  ,
                        temperature     = 0.72
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json        = JsonSerializer.Serialize(body, jsonOptions);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var url      = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var response = await client.PostAsync(url, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CHAT ERROR] {response.StatusCode}: {err}");
                    return Json(new { reply = "Xin lỗi, tôi đang gặp sự cố. Vui lòng thử lại." });
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc    = JsonDocument.Parse(responseJson);

                var reply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Xin lỗi, không có phản hồi.";

                return Json(new { reply, isSportMode = isSportIntent });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHAT EXCEPTION] {ex.Message}");
                return Json(new { reply = "Xin lỗi, tôi đang gặp sự cố kỹ thuật. Vui lòng thử lại sau." });
            }
        }

        // ── Phát hiện từ khoá thể thao / tốc độ ─────────────────────────
        private static bool ContainsSportKeywords(string text)
        {
            var keywords = new[]
            {
                "tốc độ", "nhanh", "mạnh", "thể thao", "sport", "pkl", "phân khối lớn",
                "mạo hiểm", "đua xe", "racing", "drift", "stunt", "adrenaline",
                "cảm giác mạnh", "hú ga", "siêu xe", "superbike", "streetfighter",
                "r15", "r3", "cbr", "zx", "ninja", "gs", "mt-", "300cc", "400cc",
                "650cc", "750cc", "1000cc", "cafe racer", "naked bike"
            };
            return keywords.Any(kw => text.Contains(kw));
        }
    }

    // ── Gemini Request Models ─────────────────────────────────────────────────
    public class GeminiRequest
    {
        public GeminiSystemInstruction system_instruction { get; set; } = new();
        public List<GeminiContent>     contents           { get; set; } = new();
        public GeminiGenerationConfig  generationConfig   { get; set; } = new();
    }

    public class GeminiSystemInstruction
    {
        public List<GeminiPart> parts { get; set; } = new();
    }

    public class GeminiContent
    {
        public string           role  { get; set; } = "";
        public List<GeminiPart> parts { get; set; } = new();
    }

    public class GeminiPart
    {
        public string text { get; set; } = "";
    }

    public class GeminiGenerationConfig
    {
        public int    maxOutputTokens { get; set; } = 1024;
        public double temperature     { get; set; } = 0.72;
    }

    // ── Chat Request / Message Models ────────────────────────────────────────
    public class ChatRequest
    {
        public string             Message { get; set; } = "";
        public List<ChatMessage>? History { get; set; }
    }

    public class ChatMessage
    {
        public string Role    { get; set; } = "";
        public string Content { get; set; } = "";
    }
}