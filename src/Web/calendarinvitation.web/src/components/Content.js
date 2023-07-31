import React, { useState, useEffect, useRef, useCallback, buildMessage } from "react";

import { Calendar, momentLocalizer, Views } from "react-big-calendar";
import moment from "moment";
import { useAuth0 } from "@auth0/auth0-react";
import CustomEvent from "./CustomEvent";
import "react-big-calendar/lib/css/react-big-calendar.css";

const Content = () => {
  const localizer = momentLocalizer(moment);
  const apiOrigin = "http://localhost:5020"; // TODO move to config
  const { user } = useAuth0();
  const { getAccessTokenSilently } = useAuth0();
  const clickRef = useRef(null);
  const [state, setState] = useState({
    events: [],
    apiMessage: "",
    error: null
  });


  useEffect(() => {
    getAllEvents(user.sub);

    return () => {
      window.clearTimeout(clickRef?.current)
    }
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
          title: "Some title"
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

  const onSelectEvent = useCallback((calEvent) => {
    /**
     * Here we are waiting 250 milliseconds (use what you want) prior to firing
     * our method. Why? Because both 'click' and 'doubleClick'
     * would fire, in the event of a 'doubleClick'. By doing
     * this, the 'click' handler is overridden by the 'doubleClick'
     * action.
     */
    window.clearTimeout(clickRef?.current);
    // clickRef.current = window.setTimeout(() => {
      window.alert(JSON.stringify(calEvent))
    // }, 250);
  }, []);

  const onDoubleClickEvent = useCallback((calEvent) => {
    /**
     * Notice our use of the same ref as above.
     */
    window.clearTimeout(clickRef?.current)
    // clickRef.current = window.setTimeout(() => {
      window.alert(JSON.stringify(calEvent))
    // }, 250);
  }, []);

  return (
    <Calendar
      localizer={localizer}
      defaultDate={new Date()}
      defaultView={Views.WEEK}
      events={state.events}
      // components={{
      //   event: CustomEvent // use your custom event component
      // }}
      onDoubleClickEvent={onDoubleClickEvent}
      onSelectEvent={onSelectEvent}
      style={{ height: "70vh", marginBottom: "2vh" }}
    />
  );
}

export default Content;
