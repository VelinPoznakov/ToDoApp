import { Link } from "react-router-dom";
import {useEffect, useState} from "react";
import type { Todo } from "../types/todo";
import SearchBar from "../Components/layout/SearchBar";
import {useTodos} from "../Components/layout/FetchComponenet.tsx";

function HomePage() {
    const [page, setPage] = useState(1);
    const pageSize = 5; // must match backend default

    const [search, setSearch] = useState("");

    const { data, isFetching, isError } = useTodos(search, page, pageSize);

    useEffect(() => {
        setPage(1);
    }, [search]);

    return (
      <div className="container">
        <SearchBar onSearch={setSearch} />

        <div className="d-flex align-items-center gap-2 mb-3">
          <h1 className="mb-0">Todos</h1>
          {isFetching && <small>Searching...</small>}
        </div>

        {isError && <div>Error loading todos</div>}

        {data?.map((todo: Todo) => (
            <div key={todo.id} className="card mb-3">
              <div className="card-body d-flex justify-content-between align-items-center">
                <h5 className="mb-0">{todo.name}</h5>
                <Link
                    to={`/todos/${todo.id}`}
                    className="btn btn-sm btn-primary"
                >
                  Open
                </Link>
              </div>
            </div>
        ))}

        <div className="text-center mt-4">
          <Link to="/todos/create" className="btn btn-primary btn-lg">
            Create
          </Link>
        </div>


          <div className="d-flex justify-content-between mt-4">
              <button
                  className="btn btn-secondary"
                  disabled={page === 1}
                  onClick={() => setPage(p => p - 1)}
              >
                  Previous
              </button>

              <button
                  className="btn btn-secondary"
                  disabled={data && data.length < pageSize}
                  onClick={() => setPage(p => p + 1)}
              >
                  Next
              </button>
          </div>


      </div>
  );
}

export default HomePage;
