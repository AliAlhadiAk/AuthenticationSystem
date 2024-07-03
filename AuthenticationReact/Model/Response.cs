using System.Globalization;

namespace AuthenticationReact.Model
{
    public class Response
    {
        public string Token {  get; set; }
        public string Error {  get; set; }
        public bool Result {  get; set; }
        public string RefreshToken {  get; set; }
    }
}
