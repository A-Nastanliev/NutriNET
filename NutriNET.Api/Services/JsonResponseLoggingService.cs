namespace NutriNET.Api.Services
{
    public class JsonResponseLoggingService
    {
        private readonly RequestDelegate _next;

        public JsonResponseLoggingService(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBody = context.Response.Body;

            using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            await _next(context); 

            if (context.Response.ContentType?.Contains("application/json") == true)
            {
                memStream.Seek(0, SeekOrigin.Begin);
                var json = await new StreamReader(memStream).ReadToEndAsync();

                await using var stream = new FileStream( "responses.log", FileMode.Append, FileAccess.Write, 
                    FileShare.Read,  4096, useAsync: true);

                await using var writer = new StreamWriter(stream);
                await writer.WriteLineAsync(json);
            }

            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
    }
}
