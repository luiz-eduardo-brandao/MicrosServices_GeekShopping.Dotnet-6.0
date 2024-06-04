using GeekShopping.CartAPI.Data.ValueObjects;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GeekShopping.CartAPI.Repository
{
    public class CouponRepository : ICouponRepository
    {
        private readonly HttpClient _client;

        public CouponRepository(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<CouponVO> GetCouponByCouponCode(string couponCode, string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"api/v1/coupon/{couponCode}");

            if (response.StatusCode != HttpStatusCode.OK)
                return new CouponVO();

            var content = await response.Content.ReadAsStringAsync();

            var coupon = JsonSerializer.Deserialize<CouponVO>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

            return coupon;
        }
    }
}
