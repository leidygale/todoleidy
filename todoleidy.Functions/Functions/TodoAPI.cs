using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using todoleidy.Common.Models;
using todoleidy.Common.Responses;
using todoleidy.Functions.Entities;

namespace todoleidy.Functions.Functions
{
    public static class TodoAPI
    {
        [FunctionName(nameof(CreateTodo))]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req,
            [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log)
        {
            log.LogInformation("Recieved a new todo");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Todo todo = JsonConvert.DeserializeObject<Todo>(requestBody);

            if (string.IsNullOrEmpty(todo?.TaskDescription))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a taskDescription."
                });
            }

            TodoEntity todoEntity = new TodoEntity
            {
                CreatedTime = DateTime.UtcNow, //Devulve informacion en hora local
                ETag = "*",
                IsCompleted = false,
                PartitionKey = "TODO",
                RowKey = Guid.NewGuid().ToString(),
                TaskDescription = todo.TaskDescription
            };

            TableOperation addOperation = TableOperation.Insert(todoEntity);
            await todoTable.ExecuteAsync(addOperation);

            string message = "New todo stored in table";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            { 
               IsSuccess = true,
               Message = message,
               Result = todoEntity

            });
        }

        [FunctionName(nameof(UpdateTodo))]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req,  //para actualizar se utiliza put
            [Table("todo", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            string id,    //recibir el id
            ILogger log)
        {
            log.LogInformation($"Update for todo: {id}, received.");   //$ significa interpolacion
           
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Todo todo = JsonConvert.DeserializeObject<Todo>(requestBody);

            // validated todo id   //creamos un table operation de encontrar
            TableOperation findOperation = TableOperation.Retrieve<TodoEntity>("TODO", id);
            TableResult findResult = await todoTable.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Todo not found"
                });
            }

            //Update todo
            TodoEntity todoEntity = (TodoEntity)findResult.Result;
            todoEntity.IsCompleted = todo.IsCompleted;

            if (!string.IsNullOrEmpty (todo.TaskDescription)) 
            {
                todoEntity.TaskDescription = todo.TaskDescription;
            }

                        
            TableOperation addOperation = TableOperation.Replace(todoEntity);
            await todoTable.ExecuteAsync(addOperation);

            string message = $"Todo; {id}, update in table.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = todoEntity

            });
        }

    }
}
