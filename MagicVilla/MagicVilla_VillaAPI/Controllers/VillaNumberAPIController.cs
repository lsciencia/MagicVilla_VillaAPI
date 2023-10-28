using AutoMapper;
using MagicVilla_VillaAPI.Models.DTO;
using MagicVilla_VillaAPI.Models;
using MagicVilla_VillaAPI.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MagicVilla_VillaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VillaNumberAPIController : ControllerBase
    {
        protected APIResponse _response;
        private readonly ILogger<VillaNumberAPIController> _logger;
        private readonly IVillaNumberRepository _dbVillaNumber;
        private readonly IVillaRepository _dbVilla;
        private readonly IMapper _mapper;

        public VillaNumberAPIController(ILogger<VillaNumberAPIController> logger,
            IVillaNumberRepository dbVillaNumber,
            IVillaRepository dbVilla,
            IMapper mapper)
        {
            _logger = logger;
            this._dbVillaNumber = dbVillaNumber;
            this._dbVilla = dbVilla;
            this._mapper = mapper;
            this._response = new();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetVillaNumbers()
        {
            try
            {
                _logger.LogInformation("Getting all villa numbers");
                IEnumerable<VillaNumber> villaList = await _dbVillaNumber.GetAllAsync(includeProperties: "Villa");

                _response.Result = _mapper.Map<List<VillaNumberDTO>>(villaList);
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

        [HttpGet("{id:int}", Name = "GetVillaNumber")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetVillaNumber(int id)
        {
            try
            {
                if (id == 0)
                {
                    _logger.LogInformation($"Get villa error with id {id}");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var villa = await _dbVillaNumber.GetAsync(x => x.VillaNo == id, includeProperties: "Villa");
                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<VillaNumberDTO>(villa);
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
        public async Task<ActionResult<APIResponse>> CreateVillaNumber([FromBody] VillaNumberCreateDTO createDTO)
        {
            try
            {
                if (await _dbVillaNumber.GetAsync(x =>
                        x.VillaNo == createDTO.VillaNo
                    ) != null)
                {
                    ModelState.AddModelError("ErrorMessages", "Villa already exists!");
                    return BadRequest(ModelState);
                }

                if (await _dbVilla.GetAsync(x => x.Id == createDTO.VillaNo) == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Villa ID is invalid!");
                    return BadRequest(ModelState);
                }

                if (createDTO == null)
                {
                    return BadRequest(createDTO);
                }

                VillaNumber villa = _mapper.Map<VillaNumber>(createDTO);

                await _dbVillaNumber.CreateAsync(villa);


                _response.Result = _mapper.Map<VillaNumberDTO>(villa);
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetVillaNumber", new { id = villa.VillaNo }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages =
                    new List<string> { ex.ToString() };
                return BadRequest(_response);
            }
        }

        [HttpDelete("{id:int}", Name = "DeleteVillaNumber")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> DeleteVillaNumber(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                var villa = await _dbVillaNumber.GetAsync(x => x.VillaNo == id);
                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                await _dbVillaNumber.RemoveAsync(villa);

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

        [HttpPut("{id:int}", Name = "UpdateVillaNumber")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdateVilla(int id, [FromBody] VillaNumberUpdateDTO updateDTO)
        {
            try
            {
                if (id != updateDTO.VillaNo || updateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                if (await _dbVilla.GetAsync(x => x.Id == updateDTO.VillaNo) == null)
                {
                    ModelState.AddModelError("ErrorMessages", "Villa ID is invalid!");
                    return BadRequest(ModelState);
                }

                VillaNumber model = _mapper.Map<VillaNumber>(updateDTO);
                await _dbVillaNumber.UpdateAsync(model);

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

        [HttpPatch("{id:int}", Name = "UpdatePartialVillaNumber")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePartialVilla(int id, JsonPatchDocument<VillaNumberUpdateDTO> patchDTO)
        {
            try
            {
                if (id == 0 || patchDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var villa = await _dbVillaNumber.GetAsync(x => x.VillaNo == id, tracked: false);

                VillaNumberUpdateDTO villaDTO = _mapper.Map<VillaNumberUpdateDTO>(villa);

                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }
                patchDTO.ApplyTo(villaDTO, ModelState);

                VillaNumber model = _mapper.Map<VillaNumber>(villaDTO);

                await _dbVillaNumber.UpdateAsync(model);

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
