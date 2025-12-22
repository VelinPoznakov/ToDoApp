export async function getTodos(
    search: string,
    page: number,
    pageSize: number
) {
  const params = new URLSearchParams();

  if (search) params.append("search", search);
  params.append("number", page.toString());
  params.append("pageSize", pageSize.toString());

  const response = await fetch(
      `http://localhost:5166/api/todos?${params.toString()}`
  );

  if (!response.ok) {
    throw new Error("Failed to fetch todos");
  }

  return response.json();
}




export type CreateTodoDto = {
  name: string;
  description: string;
};

export async function createTodo(data: CreateTodoDto) {
  const response = await fetch("http://localhost:5166/api/todos", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    throw new Error("Failed to create todo");
  }

  return response.json();
}

export async function getTodoById(id: string){
  const response = await fetch(`http://localhost:5166/api/todos/${id}`);

  if(!response.ok){
    throw new Error("Failed to fetch todos");
  }

  return response.json();

}

export async function deleteTodo(id: string) {
  const response = await fetch(
    `http://localhost:5166/api/todos/${id}`,
    {
      method: "DELETE",
    }
  );

  if (!response.ok) {
    throw new Error("Failed to delete todo");
  }
}

export type UpdateTodoDto = {
  name: string;
  description: string;
};

export async function updateTodo(
  id: string,
  data: UpdateTodoDto
) {
  const response = await fetch(
    `http://localhost:5166/api/todos/${id}`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(data),
    }
  );

  if (!response.ok) {
    throw new Error("Failed to update todo");
  }

  return response.json();
}

