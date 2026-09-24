using EmployeeAPI.Models;
using EmployeeAPI.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EmployeeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;
        public EmployeeController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }
        // GET: api/<EmployeeController>
        [HttpGet]
        public async Task<List<Employee>> Get([FromQuery] string? departmentName, [FromQuery] string? positionName) =>
            await _mongoDBService.GetAsync(departmentName, positionName);

        // GET api/<EmployeeController>/5
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Employee>> Get(string id)
        {
            var Employee = await _mongoDBService.GetAsync(id);

            if (Employee is null)
            {
                return NotFound();
            }

            return Employee;
        }

        [HttpGet("departments")]
        public async Task<List<Department>> GetDepartments() =>
            await _mongoDBService.GetDepartmentsAsync();

        [HttpGet("departments/{departmentId}/positions")]
        public async Task<List<Position>> GetPositions(string departmentId) =>
            await _mongoDBService.GetPositionsByDepartmentIdAsync(departmentId);

        // POST api/<EmployeeController>
        [HttpPost]
        public async Task<IActionResult> Post(Employee newEmployee)
        {
            if (newEmployee.Id is not null)
            {
                return BadRequest();
            }
            // Verificar si ya existe un empleado con el mismo Email
            var existingEmployee = await _mongoDBService.GetByEmailAsync(newEmployee.Email);
            if (existingEmployee is not null)
            {
                return Conflict($"An employee with the email '{newEmployee.Email}' already exists.");
            }

            // Verificar si el departamento ya existe
            var department = await _mongoDBService.GetDepartmentByNameAsync(newEmployee.Department);
            if (department is null)
            {
                // Crear el departamento si no existe
                department = await _mongoDBService.CreateDepartmentAsync(newEmployee.Department);
            }

            // Asignar el ID del departamento al empleado
            newEmployee.Department_Id = department.Id;

            // Verificar si el puesto ya existe en el departamento
            var position = await _mongoDBService.GetPositionByNameAndDepartmentAsync(newEmployee.Position, department.Id);
            if (position is null)
            {
                // Crear el puesto si no existe
                position = await _mongoDBService.CreatePositionAsync(newEmployee.Position, department.Id);
            }

            // Asignar el ID del puesto al empleado
            newEmployee.Position_Id = position.Id;

            try
            {
                await _mongoDBService.CreateAsync(newEmployee);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return Conflict($"An employee with the email '{newEmployee.Email}' already exists.");
            }

            return CreatedAtAction(nameof(Get), new { id = newEmployee.Id }, newEmployee);
        }

        // PUT api/<EmployeeController>/5
        [HttpPut("{id:length(24)}")]
        public async Task<ActionResult<Employee>> Update(string id, Employee updatedEmployee)
        {
            var Emp = await _mongoDBService.GetAsync(id);

            if (Emp is null)
            {
                return NotFound();
            }

            updatedEmployee.Id = Emp.Id;

            await _mongoDBService.UpdateAsync(id, updatedEmployee);

            return updatedEmployee;
        }

        // DELETE api/<EmployeeController>/5
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var book = await _mongoDBService.GetAsync(id);

            if (book is null)
            {
                return NotFound();
            }

            await _mongoDBService.RemoveAsync(id);

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<Employee>> UpdateEmployee(string id, PatchEmployee employee)
        {
            // Obtener los campos del body
            employee.Id = id;
            var updatedEmployee = employee.ToDictionary();

            // Actualizar el objeto
            var employeeFromDb = await _mongoDBService.GetAsync(id);

            if (employeeFromDb == null)
            {
                return NotFound();
            }
            
            foreach (var key in updatedEmployee.Keys)
            {
                if (typeof(Employee).GetProperty(key) != null)
                {
                    if (updatedEmployee[key] != null)
                    {
                        typeof(Employee).GetProperty(key).SetValue(employeeFromDb, updatedEmployee[key]);
                    }
                }
            }

            await _mongoDBService.UpdateAsync(id, employeeFromDb);

            return employeeFromDb;
        }
    }
}
