import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getTodoById, deleteTodo } from "../api/todos";
import type { Todo } from "../types/todo";

function TodoDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // ✅ ALL hooks FIRST (no conditions)
  const {
    data: todo,
    isLoading,
    isError,
  } = useQuery<Todo>({
    queryKey: ["todo", id],
    queryFn: () => getTodoById(id as string),
    enabled: !!id, // ⬅️ prevents running when id is undefined
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteTodo(id as string),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["todos"] });
      navigate("/");
    },
  });

  // ✅ AFTER hooks: safety & UI states
  if (!id) {
    return <div className="container mt-4">Invalid todo id</div>;
  }

  if (isLoading) {
    return <div className="container mt-4">Loading...</div>;
  }

  if (isError || !todo) {
    return <div className="container mt-4">Todo not found</div>;
  }

  return (
    <div className="container mt-4">
      <h2 className="text-center">{todo.name}</h2>

      <p className="mt-3">{todo.description}</p>

      <div className="mt-4">
        <Link
          to={`/todos/${todo.id}/edit`}
          className="btn btn-warning me-2"
        >
          Edit
        </Link>

        <button
          className="btn btn-danger"
          onClick={() => deleteMutation.mutate()}
          disabled={deleteMutation.isPending}
        >
          {deleteMutation.isPending ? "Deleting..." : "Delete"}
        </button>
      </div>
    </div>
  );
}

export default TodoDetailsPage;
