using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Entities;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
        {
            _itemsRepository = itemsRepository;
            _publishEndpoint = publishEndpoint;
        }


        [HttpGet]
        public async Task<ActionResult<ItemDto>> GetItems()
        {
            var items = (await _itemsRepository.GetAllAsync()).Select(item => item.AsDto());
            return Ok(items);
        }

        [HttpGet("{id:guid}")]
        public async  Task<ActionResult<ItemDto>> GetItemById(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>>  CreateItem(CreateItemDto requestItem)
        {
            var item = new Item
            {
                Name = requestItem.Name,
                Description = requestItem.Description,
                Price = requestItem.Price,
                CreatedDate = DateTimeOffset.Now
                
            };

            await _itemsRepository.CreateAsync(item);

            await _publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));
            
            return CreatedAtAction(nameof(GetItemById), new { Id = item.Id }, item);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult>  UpdateItem(Guid id, UpdateItemDto requestItem)
        {
            var existingItem = await _itemsRepository.GetAsync(id);

            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = requestItem.Name;
            existingItem.Description = requestItem.Description;
            existingItem.Price = requestItem.Price;

            await _itemsRepository.UpdateAsync(existingItem);
            
            await _publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));


            return NoContent();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult>  RemoveItem(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            await _itemsRepository.RemoveAsync(item.Id);
            
            await _publishEndpoint.Publish(new CatalogItemDeleted(item.Id));

            return NoContent();
        }
    }
}
