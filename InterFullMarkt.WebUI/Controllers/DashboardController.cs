using Microsoft.AspNetCore.Mvc;
using MediatR;
using InterFullMarkt.Application.Features.Dashboard.Queries;

namespace InterFullMarkt.WebUI.Controllers;

public class DashboardController : Controller
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _mediator.Send(new GetDashboardQuery());
        return View(result);
    }
}