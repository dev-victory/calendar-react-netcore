import React, { useState, useEffect, useCallback } from "react";
import {useHistory} from 'react-router-dom';

import { Calendar, momentLocalizer, Views } from "react-big-calendar";
import moment from "moment";
import { useAuth0 } from "@auth0/auth0-react";
import "react-big-calendar/lib/css/react-big-calendar.css";
import Modal from "./Modal";

const Content = () => {
  const localizer = momentLocalizer(moment);
  const apiOrigin = "http://localhost:5020"; // TODO move to config
  const { user } = useAuth0();
  const { getAccessTokenSilently } = useAuth0();
  const history = useHistory();
  const [isOpen, setIsOpen] = useState(false);

  const [eventData, setEventData] = useState({
    start: '',
    end: ''
  });

  const [state, setState] = useState({
    events: [],
    apiMessage: "",
    error: null
  });


  useEffect(() => {
    getAllEvents(user.sub);
  }, []);

  const getAllEvents = async (userId) => {
    try {
      const token = await getAccessTokenSilently();

      const response = await fetch(`${apiOrigin}/Event/GetEventsByUser/${userId}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      const responseData = await response.json();
      const mappedCalendarEvents = responseData.map((e) => {
        return {
          id: e.eventId,
          start: new Date(e.startDate),
          end: new Date(e.endDate),
          title: e.name
        };
      });

      setState({
        ...state,
        events: mappedCalendarEvents,
      });
    } catch (error) {
      setState({
        ...state,
        error: error.error,
      });
    }
  };

  const onDoubleClickEvent = useCallback((calEvent) => {
    history.push(`/event-details/${calEvent.id}`);
  }, []);

  const openAddEventModal = useCallback(({ start, end }) => {
    setEventData({
      start: start.toString(),
      end: end.toString()
    });

    setIsOpen(true);
  }, []);

  return (
    <>
      <Calendar
        localizer={localizer}
        defaultDate={new Date()}
        defaultView={Views.WEEK}
        events={state.events}
        onSelectSlot={openAddEventModal}
        onDoubleClickEvent={onDoubleClickEvent}
        selectable
        style={{ height: "70vh", marginBottom: "2vh" }}
      />
      {isOpen && <Modal setIsOpen={setIsOpen} eventData={eventData} />}
    </>
  );
}

export default Content;
