import React, { Component } from "react";


class CustomEvent extends Component {
    render() {
      const { event } = this.props;

      

      return (
        <div id={event.id}>
          {event.title}
        </div>
      );
    }
  }

  export default CustomEvent;