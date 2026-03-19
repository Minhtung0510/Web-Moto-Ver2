using Microsoft.AspNetCore.Mvc;
using MotoBikeStore.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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
                return Json(new { reply = "Vui lòng nhập câu hỏi.", products = Array.Empty<object>() });

            // ── Load products & categories ────────────────────────────────
            var products   = _db.Products.ToList();
            var categories = _db.Categories.ToList();

            // ── Build catalog text ────────────────────────────────────────
            var catalogText = new StringBuilder();
            catalogText.AppendLine("DANH SÁCH SẢN PHẨM (dùng ID khi gợi ý):");
            catalogText.AppendLine("---");
            foreach (var cat in categories)
            {
                var catProds = products.Where(p => p.CategoryId == cat.Id).ToList();
                if (!catProds.Any()) continue;
                catalogText.AppendLine($"\n[{cat.Name.ToUpper()}]");
                foreach (var p in catProds)
                {
                    catalogText.AppendLine(
                        $"- ID:{p.Id} | {p.Name} | {p.Brand} | {p.Price:N0}₫" +
                        (p.OldPrice.HasValue ? $" (gốc {p.OldPrice:N0}₫)" : "") +
                        $" | {p.Engine}" +
                        (p.Stock > 0 ? $" | Còn {p.Stock}" : " | HẾT HÀNG")
                    );
                }
            }

            // ── Intent detection ──────────────────────────────────────────
            var fullText = string.Join(" ",
                (request.History ?? new List<ChatMessage>())
                    .Select(h => h.Content)
                    .Append(request.Message)
            ).ToLower();
            bool isSport = ContainsSportKeywords(fullText);

            // ── System Prompt ─────────────────────────────────────────────
            var systemPrompt = $@"Bạn là chuyên gia tư vấn xe máy tại MotoBike Store.

QUY TRÌNH TƯ VẤN:
1. KHÁM PHÁ: Hỏi mục đích, ngân sách, kinh nghiệm lái nếu chưa rõ.
2. PHÂN TÍCH: Đối chiếu với danh sách sản phẩm.
3. GỢI Ý: Đề xuất 2-3 xe kèm lý do. Luôn ghi rõ ID sản phẩm khi gợi ý, ví dụ [ID:5].

{(isSport ? @"⚠️ KHÁCH ĐAM MÊ TỐC ĐỘ/THỂ THAO:
Bắt buộc tư vấn an toàn TRƯỚC: mũ full-face, áo giáp, găng tay, giày riding.
Gợi ý: 'Tôi hiểu đam mê tốc độ! Nhưng trước tiên hãy đảm bảo an toàn...'
Sau đó mới giới thiệu xe có ABS/traction control phù hợp kinh nghiệm.
" : "")}
{catalogText}

NGUYÊN TẮC:
- Trả lời HOÀN CHỈNH, không bỏ lửng. Tối đa 150 từ.
- Khi gợi ý xe, ghi ID dạng [ID:số] để hệ thống nhận diện.
- Kết thúc bằng 1 câu hỏi gợi mở.
- Tiếng Việt tự nhiên, dùng emoji hợp lý.
- KHÔNG bịa sản phẩm ngoài danh sách.";

            // ── Gemini API ────────────────────────────────────────────────
            var apiKey = _config["Gemini:ApiKey"] ?? "";
            if (string.IsNullOrWhiteSpace(apiKey))
                return Json(new { reply = "Chatbot chưa được cấu hình.", products = Array.Empty<object>() });

            var model = _config["Gemini:Model"] ?? "gemini-2.0-flash";

            try
            {
                var client  = _httpClientFactory.CreateClient();
                var history = (request.History ?? new List<ChatMessage>()).TakeLast(6).ToList();

                var contents = new List<GeminiContent>();
                foreach (var h in history)
                    contents.Add(new GeminiContent
                    {
                        role  = h.Role == "assistant" ? "model" : "user",
                        parts = new List<GeminiPart> { new() { text = h.Content } }
                    });
                contents.Add(new GeminiContent
                {
                    role  = "user",
                    parts = new List<GeminiPart> { new() { text = request.Message } }
                });

                var body = new GeminiRequest
                {
                    system_instruction = new GeminiSystemInstruction
                    {
                        parts = new List<GeminiPart> { new() { text = systemPrompt } }
                    },
                    contents         = contents,
                    generationConfig = new GeminiGenerationConfig { maxOutputTokens = 2048, temperature = 0.72 }
                };

                var opts     = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                var json     = JsonSerializer.Serialize(body, opts);
                var httpBody = new StringContent(json, Encoding.UTF8, "application/json");
                var url      = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var response = await client.PostAsync(url, httpBody);

                // Retry once on 429
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(5000);
                    httpBody = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await client.PostAsync(url, httpBody);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CHAT ERROR] {response.StatusCode}: {err}");
                    return Json(new { reply = "Xin lỗi, tôi đang gặp sự cố. Vui lòng thử lại.", products = Array.Empty<object>() });
                }

                var respJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(respJson);
                var reply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Xin lỗi, không có phản hồi.";

                // ── Trích xuất product IDs từ reply [ID:x] ────────────────
                var mentionedProducts = ExtractProductsFromReply(reply, products);

                return Json(new
                {
                    reply,
                    isSportMode = isSport,
                    products    = mentionedProducts
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHAT EXCEPTION] {ex.Message}");
                return Json(new { reply = "Xin lỗi, tôi đang gặp sự cố kỹ thuật. Vui lòng thử lại sau.", products = Array.Empty<object>() });
            }
        }

        // ── Trích xuất sản phẩm được đề cập trong reply ──────────────────
        private static List<object> ExtractProductsFromReply(string reply, List<Product> allProducts)
        {
            var result = new List<object>();
            // Tìm tất cả [ID:n] trong reply
            var matches = Regex.Matches(reply, @"\[ID:(\d+)\]");
            var seenIds = new HashSet<int>();

            foreach (Match m in matches)
            {
                if (int.TryParse(m.Groups[1].Value, out int id) && !seenIds.Contains(id))
                {
                    var p = allProducts.FirstOrDefault(x => x.Id == id);
                    if (p != null)
                    {
                        seenIds.Add(id);
                        result.Add(new
                        {
                            id       = p.Id,
                            name     = p.Name,
                            price    = p.Price,
                            imageUrl = p.ImageUrl,
                            brand    = p.Brand
                        });
                    }
                }
            }

            // Fallback: nếu AI không dùng [ID:x], tìm tên sản phẩm trong reply
            if (!result.Any())
            {
                foreach (var p in allProducts)
                {
                    if (reply.Contains(p.Name, StringComparison.OrdinalIgnoreCase) && !seenIds.Contains(p.Id))
                    {
                        seenIds.Add(p.Id);
                        result.Add(new
                        {
                            id       = p.Id,
                            name     = p.Name,
                            price    = p.Price,
                            imageUrl = p.ImageUrl,
                            brand    = p.Brand
                        });
                        if (result.Count >= 3) break; // tối đa 3 card
                    }
                }
            }

            return result;
        }

        // ── Sport keyword detection ───────────────────────────────────────
        private static bool ContainsSportKeywords(string text)
        {
            var kw = new[] {
                "tốc độ","nhanh","mạnh","thể thao","sport","pkl","phân khối lớn",
                "mạo hiểm","đua xe","racing","drift","stunt","adrenaline",
                "cảm giác mạnh","hú ga","siêu xe","superbike","streetfighter",
                "r15","r3","cbr","zx","ninja","gs","mt-","300cc","400cc",
                "650cc","750cc","1000cc","cafe racer","naked bike"
            };
            return kw.Any(k => text.Contains(k));
        }
    }

    // ── Gemini Models ─────────────────────────────────────────────────────────
    public class GeminiRequest
    {
        public GeminiSystemInstruction system_instruction { get; set; } = new();
        public List<GeminiContent>     contents           { get; set; } = new();
        public GeminiGenerationConfig  generationConfig   { get; set; } = new();
    }
    public class GeminiSystemInstruction { public List<GeminiPart> parts { get; set; } = new(); }
    public class GeminiContent           { public string role { get; set; } = ""; public List<GeminiPart> parts { get; set; } = new(); }
    public class GeminiPart              { public string text { get; set; } = ""; }
    public class GeminiGenerationConfig  { public int maxOutputTokens { get; set; } = 2048; public double temperature { get; set; } = 0.72; }

    // ── Chat Models ───────────────────────────────────────────────────────────
    public class ChatRequest  { public string Message { get; set; } = ""; public List<ChatMessage>? History { get; set; } }
    public class ChatMessage  { public string Role { get; set; } = ""; public string Content { get; set; } = ""; }
}