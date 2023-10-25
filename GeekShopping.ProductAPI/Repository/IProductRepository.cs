using GeekShopping.ProductAPI.Data.ValueObjects;

namespace GeekShopping.ProductAPI.Repository
{
    public interface IProductRepository
    {
        public Task<IEnumerable<ProductVO>> FindAll();
        public Task<ProductVO> FindById(long id);
        public Task<ProductVO> Create(ProductVO productVO);
        public Task<ProductVO> Update(ProductVO productVO);
        public Task<bool> Delete(long id);
    }
}
