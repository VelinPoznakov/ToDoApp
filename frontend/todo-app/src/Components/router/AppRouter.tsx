import { createBrowserRouter } from "react-router-dom"
import HomePage from "../../pages/HomePage";
import AboutPage from "../../pages/AboutPage";
import TodoDetailsPage from "../../pages/TodoDetailsPage";
import CreateTodoPage from "../../pages/CreateTodoPage";
import UpdateTodoPage from "../../pages/UpdateTodoPage";
import AppLayout from "../layout/AppLayout";


export const router = createBrowserRouter([

  {
    element: <AppLayout />,
    children: [

      {
        path: "/",
        element: <HomePage/>
      },
      {
        path: "/about",
        element: <AboutPage/>
      },
      {
        path: "/todos/:id",
        element: <TodoDetailsPage/>
      },
      {
        path: "/todos/create",
        element: <CreateTodoPage/>
      },
      {
        path: "/todos/:id/edit",
        element: <UpdateTodoPage/>
      }

    ]
  },


  
]);