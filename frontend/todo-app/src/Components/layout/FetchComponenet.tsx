import { useQuery } from "@tanstack/react-query";
import type {Todo} from "../../types/todo.ts";
import {getTodos} from "../../api/todos.ts";


export function useTodos(search: string, page: number, pageSize: number) {
    return useQuery<Todo[]>({
        queryKey: ["todos", search, page, pageSize],
        queryFn: () => getTodos(search, page, pageSize),
        placeholderData: (prev) => prev,
    });
}
