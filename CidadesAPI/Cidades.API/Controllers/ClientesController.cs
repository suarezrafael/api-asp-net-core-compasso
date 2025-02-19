﻿using AutoMapper;
using Cidades.API.Models;
using Cidades.API.ResourcesParameters;
using Cidades.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Cidades.API.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    public class ClientesController : ControllerBase
    {
        private readonly ILogger<ClientesController> _logger;
        private readonly IApiRepository _apiRepository;
        private readonly IMapper _mapper;

        public ClientesController(ILogger<ClientesController> logger, IApiRepository apiRepository,
            IMapper mapper)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            _apiRepository = apiRepository ??
                throw new ArgumentNullException(nameof(apiRepository));

            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Cadastrar um novo cliente
        /// </summary>
        /// <param name="cliente"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ClienteDto> CreateCliente(ClienteParaCriacaoDto cliente)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var clienteEntidade = _mapper.Map<Entities.Cliente>(cliente);
                _apiRepository.AddCliente(clienteEntidade);
                _apiRepository.Save();

                var clienteParaRetorno = _mapper.Map<ClienteDto>(clienteEntidade);

                return CreatedAtRoute("GetCliente",
                    new { clienteId = clienteParaRetorno.Id },
                    clienteParaRetorno);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"CreateCliente: Exceção ao cadastrar cliente com os seguintes parametros:  {cliente.ToString()}", ex);
                return StatusCode(500, "Ocorreu um problema ao lidar com sua solicitação.");
            }
        }

        /// <summary>
        /// Consultar um cliente pelo Nome parcial
        /// </summary>
        /// <param name="clientesResourceParameters"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpHead]
        public ActionResult<IEnumerable<ClienteDto>> GetClientes(
            [FromQuery] ClientesResourceParameters clientesResourceParameters)
        {
            try
            {
                var clientesEntidade = _apiRepository.GetClientes(clientesResourceParameters);
                return Ok(_mapper.Map<IEnumerable<ClienteDto>>(clientesEntidade));
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"GetClientes: Exceção ao consultar cliente com os seguintes parametros:  {clientesResourceParameters.ToString()}", ex);
                return StatusCode(500, "Ocorreu um problema ao lidar com sua solicitação.");
            }
        }

        /// <summary>
        /// Consultar um cliente específico pelo ID
        /// </summary>
        /// <param name="clienteId"></param>
        /// <returns></returns>
        [HttpGet("{clienteId}", Name = "GetCliente")]
        public IActionResult GetCliente(Guid clienteId)
        {
            try
            {
                //throw new Exception("Teste Exception 123.");
                var clienteEntidade = _apiRepository.GetCliente(clienteId);

                if (clienteEntidade == null)
                {
                    return NotFound();
                }

                return Ok(_mapper.Map<ClienteDto>(clienteEntidade));
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"GetCliente: Exceção ao consultar cliente com seguinte id:  {clienteId}", ex);
                return StatusCode(500, "Ocorreu um problema ao lidar com sua solicitação.");
            }
        }


        /// <summary>
        /// Excluir um Cliente específico
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public IActionResult DeleteCliente(Guid id)
        {
            try
            {
                var clienteEntidade = _apiRepository
                .GetCliente(id);

                if (clienteEntidade == null)
                {
                    return NotFound();
                }

                _apiRepository.DeleteCliente(clienteEntidade);

                _apiRepository.Save();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"DeleteCliente: Exceção ao excluir cliente com seguinte id:  {id}", ex);
                return StatusCode(500, "Ocorreu um problema ao lidar com sua solicitação.");
            }
        }


        /// <summary>
        /// Alterar parcialmente um cliente específico
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patchDoc"></param>
        /// <returns></returns>
        [HttpPatch("{id}")]
        public IActionResult UpdateCliente(Guid id,
            [FromBody] JsonPatchDocument<ClienteParaAtualizacaoDto> patchDoc)
        {
            try
            {
                var clienteEntidade = _apiRepository
                .GetCliente(id);

                if (clienteEntidade == null)
                {
                    return NotFound();
                }

                var clientePatch = _mapper.Map<ClienteParaAtualizacaoDto>(clienteEntidade);

                patchDoc.ApplyTo(clientePatch,ModelState);

                if (!TryValidateModel(clientePatch))
                {
                    return ValidationProblem(ModelState);
                }

                _mapper.Map(clientePatch, clienteEntidade);

                _apiRepository.UpdateCliente(clienteEntidade);

                _apiRepository.Save();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"UpdateCliente: Exceção ao atualizar cliente com seguinte id:  {id}", ex);
                return StatusCode(500, "Ocorreu um problema ao lidar com sua solicitação.");
            }
        }

        /// <summary>
        /// Sobrescrevendo ValidationProblem para retornar mensagem de detalhe da validação e a url da instancia
        /// </summary>
        /// <param name="modelStateDictionary"></param>
        /// <returns></returns>
        public override ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
        {
            var options = HttpContext.RequestServices
                .GetRequiredService<IOptions<ApiBehaviorOptions>>();
            return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
        }
    }
}
