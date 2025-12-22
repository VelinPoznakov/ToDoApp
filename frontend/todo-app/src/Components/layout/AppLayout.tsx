import { Outlet } from "react-router-dom";
import Navbar from "./Navbar";
import Footer from "./CustomFooter";

function AppLayout() {
  return (
    <div className="d-flex flex-column min-vh-100">
      <Navbar />

      <main className="container my-4 flex-fill">
        <Outlet />
      </main>

      <Footer />
    </div>
  );
}

export default AppLayout;
