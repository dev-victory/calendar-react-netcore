import React from "react";
import { faCalendarWeek } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

const Footer = () => (
  <footer className="bg-light p-3 text-center">
    <h4>
    <FontAwesomeIcon icon={faCalendarWeek}/> Calendar
    </h4>
  </footer>
);

export default Footer;
