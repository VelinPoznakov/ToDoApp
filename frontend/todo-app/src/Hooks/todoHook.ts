import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../api/api";
import type { TodoResponseDto } from "../api/todo-api";

export function useTodo() {
  
  return useQuery<TodoResponseDto[]>({
    queryKey: ["todos"],
    queryFn: async () => api.api.todosList().then((res) => res.data),
  })
}

type UpdateTodoData = {
  id: number;
  name: string;
  description: string;
}

export function UpdateTodo(){

  const qc = useQueryClient();

  return useMutation({
    mutationKey: ["updateTodo"],
    mutationFn: async (data: UpdateTodoData) => {
      const res = await api.api.todosUpdate(data.id, {
        name: data.name,
        description: data.description,
      });
      return res.data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["todos"] });
    },
    onError: (error) => {
      console.error("Error updating todo:", error);
    }
  });
}

export function CreateTodo(){
  const qc = useQueryClient();

  return useMutation({
    mutationKey: ["createTodo"],
    mutationFn: async (data: {name: string; description: string; forDate: string}) => {
      const res = await api.api.todosCreate({name: data.name, description: data.description, forDate: data.forDate});
      return res.data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["todos"] });
    },
    onError: (error) => {
      console.error("Error creating todo:", error);
    }
  })
}

type DeleteModelArgs = {
  id: number;
}

export function DeleteTodo(){
  const qc = useQueryClient();

  return useMutation({
    mutationKey: ["deleteTodo"],
    mutationFn: async(data: DeleteModelArgs) => {
      const res = await api.api.todosDelete(data.id);
      return res.data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["todos"] });
    },
    onError: (error) => {
      console.error("Error deleting todo:", error);
    }
  })
}