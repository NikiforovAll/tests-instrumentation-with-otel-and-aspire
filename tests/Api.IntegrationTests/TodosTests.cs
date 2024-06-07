namespace IntegrationTests;

using System.Net;
using Api.Todos;
using IntegrationTests.Monitoring;

[TracePerTestRun]
public class TodosTests(WebAppFixture fixture) : WebAppContext(fixture)
{
    private static readonly Func<
        FluentAssertions.Equivalency.EquivalencyAssertionOptions<TodoItem>,
        FluentAssertions.Equivalency.EquivalencyAssertionOptions<TodoItem>
    > ExcludingTodoItemFields = cfg => cfg.Excluding(p => p.Id);

    [Theory, AutoData]
    public async Task GetTodos_SomeTodosExist_Ok(string todoItemTitle)
    {
        TodoItem item = new() { Title = todoItemTitle };
        await this.AddTodo(item);

        var result = await this.Host.GetAsJson<TodoItemViewModel[]>("/todos");

        result.Should().NotBeNull();
        result.Should().ContainEquivalentOf(item, ExcludingTodoItemFields);
    }

    [Fact]
    public async Task PostTodos_ValidTodoIsCreatedInNotCompletedState_Ok()
    {
        var item = new TodoItem { Title = "I want to do this thing tomorrow" };

        var result = await this.AddTodo(item);

        result.Should().NotBeNull();
        result!.IsComplete.Should().BeFalse();
    }

    [Theory, AutoData]
    public async Task PostTodos_InvalidTitle_BadRequest(TodoItemViewModel todoItem) =>
        await this.Host.Scenario(_ =>
        {
            _.Post.Json(todoItem with { Title = string.Empty }).ToUrl("/todos");
            _.StatusCodeShouldBe(HttpStatusCode.BadRequest);
        });

    [Theory, AutoData]
    public async Task GetTodo_ExistingTodo_Ok(TodoItem item)
    {
        var dbTodo = await this.AddTodo(item);

        var result = await this.Host.GetAsJson<TodoItemViewModel>($"/todos/{dbTodo.Id}");

        result.Should().NotBeNull();

        result.Should().BeEquivalentTo(item, ExcludingTodoItemFields);
    }

    [Theory, AutoData]
    public async Task DeleteTodo_ExistingTodo_Ok(TodoItem item)
    {
        var dbTodo = await this.AddTodo(item);

        await this.Host.Scenario(_ =>
        {
            _.Delete.Url($"/todos/{dbTodo.Id}");
            _.StatusCodeShouldBeOk();
        });
    }

    [Fact]
    public Task DeleteTodo_ExistingTodo_NotFound() =>
        this.Host.Scenario(_ =>
        {
            _.Delete.Url($"/todos/{int.MaxValue}");
            _.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });

    private async Task<TodoItemViewModel> AddTodo(TodoItem item)
    {
        var result = await this.Host.Scenario(_ =>
        {
            _.Post.Json(item).ToUrl("/todos");
            _.StatusCodeShouldBe(HttpStatusCode.Created);
        });

        var output = await result.ReadAsJsonAsync<TodoItemViewModel>();

        return output!;
    }
}
