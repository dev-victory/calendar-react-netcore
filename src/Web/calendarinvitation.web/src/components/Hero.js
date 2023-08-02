import React from "react";
import { faCalendarWeek } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

const Hero = () => (
  <div className="text-center hero my-5">
    <h1 className="mb-2"><FontAwesomeIcon icon={faCalendarWeek} /> Calendar</h1>

    <p className="lead mb-2">
      Add and manage your scheduled events
    </p>
  </div>
);

export default Hero;
