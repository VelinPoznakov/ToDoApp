import { useState } from "react";
import { createTodo } from "../api/todos";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";

function CreateTodoPage(){
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState("");

  const navigate = useNavigate();
  const queryClient = useQueryClient();


  const createTodoMutation = useMutation({
    mutationFn: createTodo,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["todos"] });
      navigate("/");
    },
  });


  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    // frontend validation
    if (!name.trim()) {
      setError("Name is required");
      return;
    }

    setError("");

    createTodoMutation.mutate({
      name,
      description,
    });
  }

  return(
    <div className="container mt-4">
      <h2>Create Todo</h2>

      <form onSubmit={handleSubmit}>
        {/* NAME */}
        <div className="mb-3">
          <label className="form-label">Name</label>
          <input
            className="form-control"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>

        {/* DESCRIPTION */}
        <div className="mb-3">
          <label className="form-label">Description</label>
          <textarea
            className="form-control"
            rows={4}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        {/* ERROR MESSAGE */}
        {error && <div className="text-danger mb-2">{error}</div>}

        {/* SUBMIT BUTTON */}
        <button
          type="submit"
          className="btn btn-primary"
          disabled={createTodoMutation.isPending}
        >
          {createTodoMutation.isPending ? "Creating..." : "Create"}
        </button>
      </form>
    </div>
  )
}

export default CreateTodoPage