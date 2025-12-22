import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getTodoById, updateTodo } from "../api/todos";
import { useState, useEffect } from "react";

function UpdateTodoPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  const { data: todo, isLoading } = useQuery({
    queryKey: ["todo", id],
    queryFn: () => getTodoById(id as string),
  });

  useEffect(() => {
    if (todo) {
      setName(todo.name);
      setDescription(todo.description);
    }
  }, [todo]);

  const updateMutation = useMutation({
    mutationFn: (data: { name: string; description: string }) =>
      updateTodo(id as string, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["todos"] });
      queryClient.invalidateQueries({ queryKey: ["todo", id] });
      navigate("/");
    },
  });
  // prefill form when todo is loaded


  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    if (!name.trim()) return;

    updateMutation.mutate({ name, description });
  }

  if (!id) return <div>Invalid id</div>;

  if (isLoading) return <div>Loading...</div>;

  return (
    <div className="container mt-4">
      <h2>Edit Todo</h2>

      <form onSubmit={handleSubmit}>
        <input
          className="form-control mb-3"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />

        <textarea
          className="form-control mb-3"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />

        <button className="btn btn-warning">
          Save
        </button>
      </form>
    </div>
  );
}

export default UpdateTodoPage;
