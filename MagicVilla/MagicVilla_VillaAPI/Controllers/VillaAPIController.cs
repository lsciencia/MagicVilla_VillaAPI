using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagicVilla_VillaAPI.Controllers
{
    [Route("api/[controller]")]
    //[Route("api/VillaAPI")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        private readonly ILogger<VillaAPIController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public VillaAPIController(ILogger<VillaAPIController> logger,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            this._dbContext = dbContext;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<VillaDTO>> GetVillas()
        {
            _logger.LogInformation("Getting all villas");
            return Ok(_dbContext.Villas);
        }

        [HttpGet("{id:int}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<VillaDTO> GetVilla(int id)
        {
            if (id == 0)
            {
                _logger.LogInformation($"Get villa error with id {id}");
                return BadRequest();
            }

            var villa = _dbContext.Villas.FirstOrDefault(x => x.Id == id);
            if (villa == null)
            {
                return NotFound();
            }

            return Ok(villa);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<VillaDTO> CreateVilla([FromBody] VillaDTO villaDTO)
        {
            //if (!ModelState.IsValid)
            //{
            //    return BadRequest(ModelState);
            //}

            if (_dbContext.Villas.FirstOrDefault(x =>
                    x.Name.ToLower() == villaDTO.Name.ToLower()
                ) != null)
            {
                ModelState.AddModelError("CustomError", "Villa already exists!");
                return BadRequest(ModelState);
            }
            if (villaDTO == null)
            {
                return BadRequest(villaDTO);
            }
            if (villaDTO.Id > 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            Villa model = ConvertDTOToModel(villaDTO);


            _dbContext.Villas.Add(model);
            _dbContext.SaveChanges();

            return CreatedAtRoute("GetVilla", new { id = villaDTO.Id }, villaDTO);
        }

        [HttpDelete("{id:int}", Name = "DeleteVilla")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteVilla(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var villa = _dbContext.Villas.FirstOrDefault(x => x.Id == id);
            if (villa == null)
            {
                return NotFound();
            }

            _dbContext.Villas.Remove(villa);
            _dbContext.SaveChanges();
            return NoContent();
        }

        [HttpPut("{id:int}", Name = "UpdateVilla")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UpdateVilla(int id, [FromBody] VillaDTO villaDTO)
        {
            if (id != villaDTO.Id || villaDTO == null)
            {
                return BadRequest();
            }

            //var villa = _dbContext.Villas.FirstOrDefault(x => x.Id == id);
            //if (villa == null)
            //{
            //    return BadRequest();
            //}
            //villa.Name = villaDTO.Name;
            //villa.Sqft = villaDTO.Sqft;
            //villa.Occupancy = villaDTO.Occupancy;

            Villa model = ConvertDTOToModel(villaDTO);
            _dbContext.Villas.Update(model);
            _dbContext.SaveChanges();

            return NoContent();
        }


        [HttpPatch("{id:int}", Name = "UpdatePartialVilla")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UpdatePartialVilla(int id, JsonPatchDocument<VillaDTO> patchDTO)
        {
            if (id == 0 || patchDTO == null)
            {
                return BadRequest();
            }

            var villa = _dbContext.Villas.AsNoTracking().FirstOrDefault(x => x.Id == id);

            VillaDTO villaDTO = ConvertModelToDTO(villa);

            if (villa == null)
            {
                return BadRequest();
            }
            patchDTO.ApplyTo(villaDTO, ModelState);

            Villa model = ConvertDTOToModel(villaDTO);
            _dbContext.Update(model);
            _dbContext.SaveChanges();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return NoContent();
        }

        private VillaDTO ConvertModelToDTO(Villa? villa)
        {
            return new()
            {
                Amenity = villa.Amenity,
                Details = villa.Details,
                Id = villa.Id,
                ImageUrl = villa.ImageUrl,
                Occupancy = villa.Occupancy,
                Name = villa.Name,
                Rate = villa.Rate,
                Sqft = villa.Sqft
            };
        }

        private Villa ConvertDTOToModel(VillaDTO villaDTO)
        {
            return new()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = villaDTO.Id,
                ImageUrl = villaDTO.ImageUrl,
                Occupancy = villaDTO.Occupancy,
                Name = villaDTO.Name,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft
            };
        }
    }
}
