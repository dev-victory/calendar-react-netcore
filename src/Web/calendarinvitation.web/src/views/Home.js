import React, { Fragment } from "react";
import { withAuthenticationRequired } from "@auth0/auth0-react";
import Loading from "../components/Loading";
import Hero from "../components/Hero";
import Content from "../components/Content";

const Home = () => (
  <Fragment>
    <Hero />
    <hr />
    <Content />
  </Fragment>
);

export default withAuthenticationRequired(Home, {
  onRedirecting: () => <Loading />,
});
