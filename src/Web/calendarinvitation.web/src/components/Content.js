import React, { useState, useEffect, useCallback } from "react";
import { useHistory } from 'react-router-dom';
import { getConfig } from "../config";

import { Calendar, momentLocalizer, Views } from "react-big-calendar";
import moment from "moment";
import { useAuth0 } from "@auth0/auth0-react";
import "react-big-calendar/lib/css/react-big-calendar.css";
import Modal from "./Modal";

const Content = () => {
  const localizer = momentLocalizer(moment);
  const apiOrigin = getConfig().apiOrigin;
  const { getAccessTokenSilently } = useAuth0();
  const history = useHistory();
  const [isOpen, setIsOpen] = useState(false);
  const [reloadCalendar, setCalendarReload] = useState(false);
  const [isFetchByWeek, setIsFetchByWeek] = useState(true);

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
    getAllEvents();
  }, [reloadCalendar]);

  const getAllEvents = async () => {
    try {
      const token = await getAccessTokenSilently();

      const response = await fetch(`${apiOrigin}/Event/GetEventsByUser/${isFetchByWeek}`, {
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

  const onViewChange = useCallback((view) => {
    setIsFetchByWeek(view === Views.WEEK);
    setCalendarReload(true);
  }, []);

  const openAddEventModal = useCallback(({ start, end }) => {
    setEventData({
      start: start.toString(),
      end: end.toString()
    });
    setIsOpen(true);
  }, []);

  const onRangeChange = (range) => {
    // TODO: get by range
  }

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
        onView={onViewChange}
        onRangeChange={onRangeChange}
        style={{ height: "70vh", marginBottom: "2vh" }}
      />
      {isOpen && <Modal setIsOpen={setIsOpen} eventData={eventData} setCalendarReload={setCalendarReload} />}

    </>
  );
}

export default Content;
