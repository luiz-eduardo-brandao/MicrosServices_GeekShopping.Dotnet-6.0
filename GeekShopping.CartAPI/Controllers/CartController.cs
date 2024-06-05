using GeekShopping.CartAPI.Data.ValueObjects;
using GeekShopping.CartAPI.Messages;
using GeekShopping.CartAPI.RabbitMQSender;
using GeekShopping.CartAPI.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace GeekShopping.CartAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")] 
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _repository;
        private readonly ICouponRepository _couponRepository;
        private readonly IRabbitMQMessageSender _messageSender;

        public CartController(ICartRepository repository, ICouponRepository couponRepository, IRabbitMQMessageSender messageSender)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _couponRepository = couponRepository ?? throw new ArgumentNullException(nameof(couponRepository));
            _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        }

        [HttpGet("find-cart/{id}")]
        public async Task<ActionResult<CartVO>> FindById(string id)
        {
            var cart = await _repository.FindCartByUserId(id);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        [HttpPost("add-cart")]
        public async Task<ActionResult<CartVO>> AddCart(CartVO cartVO)
        {
            var cart = await _repository.SaveOrUpdateCart(cartVO);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        [HttpPut("update-cart")]
        public async Task<ActionResult<CartVO>> UpdateCart(CartVO cartVO)
        {
            var cart = await _repository.SaveOrUpdateCart(cartVO);
            if (cart == null) return NotFound();
            return Ok(cart);
        }

        [HttpDelete("remove-cart/{id}")]
        public async Task<ActionResult<bool>> RemoveCart(int id)
        {
            var status = await _repository.RemoveFromCart(id);
            if (!status) return BadRequest();
            return Ok(status);
        }

        [HttpPost("apply-coupon")]
        public async Task<ActionResult<CartVO>> ApplyCoupon(CartVO cartVO)
        {
            var status = await _repository.ApplyCoupon(cartVO.CartHeader.UserId, cartVO.CartHeader.CouponCode);
            if (!status) return BadRequest();
            return Ok(status);
        }

        [HttpDelete("remove-coupon/{userId}")]
        public async Task<ActionResult<CartVO>> RemoveCoupon(string userId)
        {
            var status = await _repository.RemoveCoupon(userId);
            if (!status) return BadRequest();
            return Ok(status);
        }

        [HttpPost("checkout")]
        public async Task<ActionResult<CheckoutHeaderVO>> Checkout(CheckoutHeaderVO checkoutHeader)
        {
            var token = await HttpContext.GetTokenAsync("access_token");

            if (checkoutHeader?.UserId == null) return BadRequest();
            
            var cart = await _repository.FindCartByUserId(checkoutHeader.UserId);
            
            if (cart == null) return NotFound();

            if (!string.IsNullOrEmpty(checkoutHeader.CouponCode))
            {
                var coupon = await _couponRepository.GetCouponByCouponCode(checkoutHeader.CouponCode, token);

                if (checkoutHeader.DiscountTotal != coupon.DiscountAmount)
                {
                    // 412 Precondition Failed
                    return StatusCode(412);
                }
            }

            checkoutHeader.CartDetails = cart.CartDetails;
            checkoutHeader.DateTime = DateTime.Now;

            _messageSender.SendMessage(checkoutHeader, "checkout-queue");

            await _repository.ClearCart(checkoutHeader.UserId);

            return Ok(checkoutHeader);
        }
    }
}
