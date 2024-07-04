using Equilobe.Core.Features.Loans.Commands;
using Equilobe.Core.Features.Loans.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Equilobe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LoansController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> AddLoan(LoanBookCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpPost("end")]
        public async Task<IActionResult> EndLoan(ReturnBookCommand command)
        {
            await _mediator.Send(command);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetLoans(
            [FromQuery] Guid? userId,
            [FromQuery] DateTime? loanDate,
            [FromQuery] bool? isReturned,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string sortBy = "id",
            [FromQuery] string sortDirection = "asc",
            [FromQuery] bool includeBookDetails = false)
        {
            var query = new GetLoansQuery
            {
                UserId = userId,
                LoanDate = loanDate,
                IsReturned = isReturned,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection,
                IncludeBookDetails = includeBookDetails
            };

            var loans = await _mediator.Send(query);
            return Ok(loans);
        }
    }
}
