using FinancialTrackingSystem.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialTrackingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionsController : ControllerBase
    {
        private readonly PositionsService _positionsService;

        public PositionsController(PositionsService positionsService)
        {
            _positionsService = positionsService;
        }

        // GET: api/positions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Position>>> GetPositions()
        {
            try
            {
                // Fetch positions from the service
                var positions = await _positionsService.GetPositionsFromQueue();

                if (positions == null || !positions.Any())
                {
                    return NotFound("No positions found.");
                }

                return Ok(positions);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error fetching positions: {ex.Message}");
            }
        }

        // POST: api/positions/upload
        [HttpPost("upload")]
        public async Task<ActionResult> UploadPositions()
        {
            try
            {
                // Hardcoded path for CSV upload (this can be changed)
                string filePath = @"C:\Users\User\Downloads\2025_01_29_Wakett_.NET_Developer_test_Positions.csv";

                // Ensure this is an async method in PositionsService
                await _positionsService.LoadCsv(filePath);

                return Ok("Positions uploaded successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error uploading positions: {ex.Message}");
            }
        }

        

       
    }
}
