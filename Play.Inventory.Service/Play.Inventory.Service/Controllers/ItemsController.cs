using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Extensions;

namespace Play.Inventory.Service.Controllers
{
    [Route("items")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _inventoryItemsRepository;
        private readonly IRepository<CatalogItem> _catalogItemsRepository;

        public ItemsController(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
        {
            _inventoryItemsRepository = inventoryItemsRepository;
            _catalogItemsRepository = catalogItemsRepository;
        }
        
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetItemsByUserId(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var inventoryItemsEntities = await _inventoryItemsRepository
                .GetAllAsync(item => item.UserId == userId);
            var catalogItemIds = inventoryItemsEntities.Select(item => item.CatalogItemId);
            
            var catalogItemsEntities =
                await _catalogItemsRepository.GetAllAsync(item => catalogItemIds.Contains(item.Id));

            var inventoryItemDtos = inventoryItemsEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemsEntities.Single(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> CreateItem(GrandItemsDto itemsDto)
        {
            var inventoryItem = await _inventoryItemsRepository.GetAsync(item =>
                item.UserId == itemsDto.UserId && item.CatalogItemId == itemsDto.CatalogItemId);

            if (inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = itemsDto.CatalogItemId,
                    UserId = itemsDto.UserId,
                    Quantity = itemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _inventoryItemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += itemsDto.Quantity;
                await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            return Ok();
        }

    }
}
