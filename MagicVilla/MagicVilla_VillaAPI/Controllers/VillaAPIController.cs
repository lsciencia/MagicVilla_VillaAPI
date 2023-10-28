using AutoMapper;
using Azure;
using MagicVilla_VillaAPI.Data;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Models.DTO;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace MagicVilla_VillaAPI.Controllers
{
    [Route("api/[controller]")]
    //[Route("api/VillaAPI")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        protected APIResponse _response;
        private readonly ILogger<VillaAPIController> _logger;
        private readonly IVillaRepository _dbVilla;
        private readonly IMapper _mapper;

        public VillaAPIController(ILogger<VillaAPIController> logger,
            IVillaRepository dbVilla,
            IMapper mapper)
        {
            _logger = logger;
            this._dbVilla = dbVilla;
            this._mapper = mapper;
            this._response = new();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetVillas()
        {
            try
            {
                _logger.LogInformation("Getting all villas");
                IEnumerable<Villa> villaList = await _dbVilla.GetAllAsync();

                _response.Result = _mapper.Map<List<VillaDTO>>(villaList);
                _response.StatusCode = HttpStatusCode.OK;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages =
                    new List<string> { ex.ToString() };
                return BadRequest(_response);
            }
        }

        [HttpGet("{id:int}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _logger.LogInformation($"Get villa error with id {id}");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var villa = await _dbVilla.GetAsync(x => x.Id == id);
                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<VillaDTO>(villa);
                _response.StatusCode = HttpStatusCode.OK;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages =
                    new List<string> { ex.ToString() };
                return BadRequest(_response);
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateVilla([FromBody] VillaCreateDTO createDTO)
        {
            try
            {
                if (await _dbVilla.GetAsync(x =>
                        x.Name.ToLower() == createDTO.Name.ToLower()
                    ) != null)
                {
                    ModelState.AddModelError("ErrorMessages", "Villa already exists!");
                    return BadRequest(ModelState);
                }
                if (createDTO == null)
                {
                    return BadRequest(createDTO);
                }

                Villa villa = _mapper.Map<Villa>(createDTO);

                await _dbVilla.CreateAsync(villa);


                _response.Result = _mapper.Map<VillaDTO>(villa);
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetVilla", new { id = villa.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages =
                    new List<string> { ex.ToString() };
                return BadRequest(_response);
            }
        }

        [HttpDelete("{id:int}", Name = "DeleteVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> DeleteVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                var villa = await _dbVilla.GetAsync(x => x.Id == id);
                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                await _dbVilla.RemoveAsync(villa);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages =
                    new List<string> { ex.ToString() };
                return BadRequest(_response);
            }
        }

        [HttpPut("{id:int}", Name = "UpdateVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateVilla(int id, [FromBody] VillaUpdateDTO updateDTO)
        {
            try
            {
                if (id != updateDTO.Id || updateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                Villa model = _mapper.Map<Villa>(updateDTO);
                await _dbVilla.UpdateAsync(model);

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages =
                    new List<string> { ex.ToString() };
                return BadRequest(_response);
            }
        }


        [HttpPatch("{id:int}", Name = "UpdatePartialVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePartialVilla(int id, JsonPatchDocument<VillaUpdateDTO> patchDTO)
        {
            try
            {
                if (id == 0 || patchDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var villa = await _dbVilla.GetAsync(x => x.Id == id, tracked: false);

                VillaUpdateDTO villaDTO = _mapper.Map<VillaUpdateDTO>(villa);

                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                patchDTO.ApplyTo(villaDTO, ModelState);

                Villa model = _mapper.Map<Villa>(villaDTO);

                await _dbVilla.UpdateAsync(model);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages =
                    new List<string> { ex.ToString() };
                return BadRequest(_response);
            }
        }
    }
}
