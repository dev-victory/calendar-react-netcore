import React from "react";
import loading from "../assets/loading.svg";

const Loading = () => (
  <div className="spinner">
    <img src={loading} alt="Loading" className="w-25" />
  </div>
);

export default Loading;
