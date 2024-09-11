using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using shnurok.Services.CosmosDb;

namespace shnurok.Areas.Orders.Controllers
{
	[Area("Orders")]
	[Route("api/orders")]
	[ApiController]
	public class OrdersController : ControllerBase
	{
		private readonly IContainerProvider _containerProvider;

		public OrdersController(IContainerProvider containerProvider)
		{
			_containerProvider = containerProvider;
		}


	}
}
