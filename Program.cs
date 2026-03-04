#nullable disable
using Google.GenAI;
using File = System.IO.File;
using Environment = System.Environment;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net.Mail;
using System.Net;
using OllamaSharp;
using OllamaSharp.Models.Chat;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.WebHost.UseUrls("http://localhost:5000");
var app = builder.Build();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseDefaultFiles();
app.UseStaticFiles();

// ── Welcome Email ────────────────────────────────────────────────────────────
app.MapPost("/api/welcome", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    string body = await reader.ReadToEndAsync();
    var json = JsonDocument.Parse(body);
    string email = json.RootElement.GetProperty("email").GetString();
    string name  = json.RootElement.GetProperty("name").GetString();

    try
    {
        var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential("kelkaryatharth1@gmail.com", "criu lwjf bxsz laky"),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From    = new MailAddress("kelkaryatharth1@gmail.com", "AVS"),
            Subject = "Welcome to AVS!",
            IsBodyHtml = true,
            Body = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#0a0a0f;font-family:Inter,sans-serif;'>
  <div style='max-width:520px;margin:40px auto;background:#111118;border:1px solid #2a2a3a;border-radius:16px;overflow:hidden;'>
    <div style='background:linear-gradient(135deg,#6366f1,#8b5cf6);padding:32px;text-align:center;'>
      <div style='width:52px;height:52px;background:rgba(255,255,255,0.15);border-radius:12px;display:inline-flex;align-items:center;justify-content:center;font-size:16px;font-weight:900;color:white;margin-bottom:12px;'>AVS</div>
      <h1 style='color:white;margin:0;font-size:24px;font-weight:800;letter-spacing:-0.5px;'>Welcome to AVS</h1>
    </div>
    <div style='padding:32px;'>
      <p style='color:#f1f1f5;font-size:16px;margin:0 0 16px;'>Hi {name},</p>
      <p style='color:#9999b0;font-size:14px;line-height:1.8;margin:0 0 24px;'>Your account is ready. You now have access to AVS — a multi-model AI that runs Basic, Image, and Pro responses all in one place.</p>
      <div style='background:#1a1a24;border:1px solid #2a2a3a;border-radius:12px;padding:20px;margin-bottom:24px;'>
        <div style='display:flex;align-items:center;gap:10px;margin-bottom:12px;'>
          <span style='color:#34d399;font-size:16px;'>&#9679;</span>
          <span style='color:#f1f1f5;font-size:13px;font-weight:600;'>VS-1 Basic — Fast local answers</span>
        </div>
        <div style='display:flex;align-items:center;gap:10px;margin-bottom:12px;'>
          <span style='color:#a78bfa;font-size:16px;'>&#10022;</span>
          <span style='color:#f1f1f5;font-size:13px;font-weight:600;'>VS-G1 Image — AI image generation</span>
        </div>
        <div style='display:flex;align-items:center;gap:10px;'>
          <span style='color:#fbbf24;font-size:16px;'>&#9889;</span>
          <span style='color:#f1f1f5;font-size:13px;font-weight:600;'>VS-2 Pro — Multi-model synthesized answers</span>
        </div>
      </div>
      <p style='color:#6b6b80;font-size:12px;margin:0;text-align:center;'>Your chats are saved and accessible from any device.</p>
    </div>
  </div>
</body>
</html>"
        };

        mail.To.Add(email);
        await smtp.SendMailAsync(mail);
        Console.WriteLine($"[AVS] Welcome email sent to {email}");
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[AVS] Email error: {ex.Message}");
        return Results.Ok(); // Don't block signup if email fails
    }
});

// ── Prompt ───────────────────────────────────────────────────────────────────
app.MapPost("/api/prompt", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    string body = await reader.ReadToEndAsync();
    var json = JsonDocument.Parse(body);
    string userPrompt = json.RootElement.GetProperty("prompt").GetString();
    string mode = json.RootElement.TryGetProperty("mode", out var modeEl) ? modeEl.GetString() : "pro";

    Console.WriteLine($"\n[AVS] Prompt received: {userPrompt}");
    Console.WriteLine($"[AVS] Mode: {mode}");

    if (mode == "image")
    {
        Console.WriteLine("[AVS] Image mode — generating image via Hugging Face...");
        try
        {
            string hfToken = Environment.GetEnvironmentVariable("HF_API_KEY");
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(3);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {hfToken}");
            var requestBody = JsonSerializer.Serialize(new { inputs = userPrompt });
            var content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://router.huggingface.co/hf-inference/models/black-forest-labs/FLUX.1-schnell", content);
            if (!response.IsSuccessStatusCode)
            {
                string errBody = await response.Content.ReadAsStringAsync();
                return Results.Json(new { finalAnswer = $"Image generation failed: {errBody}", sources = new Dictionary<string, string>(), imageBase64 = (string)null });
            }
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            string imageBase64 = Convert.ToBase64String(imageBytes);
            return Results.Json(new { finalAnswer = "Here is your generated image:", sources = new Dictionary<string, string>(), imageBase64 });
        }
        catch (Exception ex)
        {
            return Results.Json(new { finalAnswer = $"Image generation failed: {ex.Message}", sources = new Dictionary<string, string>(), imageBase64 = (string)null });
        }
    }

    if (mode == "basic")
    {
        Console.WriteLine("[AVS] Basic mode — querying Ollama only...");
        string basicAnswer = await OllamaChatService.RunAsync(userPrompt);
        return Results.Json(new { finalAnswer = basicAnswer, sources = new Dictionary<string, string>(), imageBase64 = (string)null });
    }

    Console.WriteLine("[AVS] Pro mode — querying all three models in parallel...");
    var client = new Client();
    Task<string> groqTask   = GroqService.RunAsync(userPrompt);
    Task<string> geminiTask = GeminiService.RunAsync(userPrompt);
    Task<string> ollamaTask = OllamaChatService.RunAsync(userPrompt);
    await Task.WhenAll(groqTask, geminiTask, ollamaTask);
    string groqResponse   = groqTask.Result;
    string geminiResponse = geminiTask.Result;
    string ollamaResponse = ollamaTask.Result;

    string combinedPrompt = $@"
You have received three different answers to the user's question: ""{userPrompt}""
Answer from Groq (Llama 3.3 70B): {groqResponse}
Answer from Gemini: {geminiResponse}
Answer from Ollama (llama3.2): {ollamaResponse}
Please synthesize all three answers into one single, comprehensive, and well-structured response.";

    var combined = await client.Models.GenerateContentAsync(model: "gemini-2.5-flash", contents: combinedPrompt);
    string finalAnswer = combined.Candidates[0].Content.Parts[0].Text;
    Console.WriteLine("[AVS] Synthesis complete.");

    return Results.Json(new {
        finalAnswer,
        sources = new Dictionary<string, string> {
            { "Groq — Llama 3.3 70B", groqResponse },
            { "Gemini 2.5 Flash",      geminiResponse },
            { "Ollama — llama3.2",     ollamaResponse }
        },
        imageBase64 = (string)null
    });
});

app.Run();

// ─── Services ────────────────────────────────────────────────────────────────

public class GroqService
{
    public static async Task<string> RunAsync(string userPrompt)
    {
        try
        {
            string apiKey   = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            string endpoint = "https://api.groq.com/openai/v1/chat/completions";
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            var requestBody = JsonSerializer.Serialize(new { model = "llama-3.3-70b-versatile", messages = new[] { new { role = "system", content = "You are a helpful assistant." }, new { role = "user", content = userPrompt } } });
            var content = new StringContent(requestBody, System.Text.Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync(endpoint, content);
            if (!response.IsSuccessStatusCode) return "";
            var parsed = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return parsed.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch (Exception ex) { Console.WriteLine($"Groq Error: {ex.Message}"); return ""; }
    }
}

public class GeminiService
{
    public static async Task<string> RunAsync(string userPrompt)
    {
        try
        {
            var client = new Client();
            var response = await client.Models.GenerateContentAsync(model: "gemini-2.5-flash", contents: userPrompt);
            return response.Candidates[0].Content.Parts[0].Text;
        }
        catch (Exception ex) { Console.WriteLine($"Gemini Error: {ex.Message}"); return ""; }
    }
}

public class OllamaChatService
{
    public static async Task<string> RunAsync(string userPrompt)
    {
        try
        {
            OllamaApiClient chatClient = new OllamaApiClient(new Uri("http://localhost:11434/"), "llama3.2");
            List<Message> chatHistory = new List<Message> { new Message(ChatRole.User, userPrompt) };
            string assistantResponse = "";
            await foreach (var stream in chatClient.ChatAsync(new ChatRequest { Model = "llama3.2", Messages = chatHistory, Stream = true }))
            {
                assistantResponse += stream?.Message?.Content ?? "";
            }
            return assistantResponse;
        }
        catch (Exception ex) { Console.WriteLine($"Ollama Error: {ex.Message}"); return ""; }
    }
}
