import React, { useState, useEffect } from "react";
import { withRouter } from "react-router";
import { useParams, useHistory } from 'react-router-dom';
import { getConfig } from "../config";
import { useAuth0, withAuthenticationRequired } from "@auth0/auth0-react";

import DateTimeRangePicker from '@wojtekmaj/react-datetimerange-picker';
import '@wojtekmaj/react-datetimerange-picker/dist/DateTimeRangePicker.css';
import 'react-calendar/dist/Calendar.css';
import 'react-clock/dist/Clock.css';

import { Formik, Form, Field, ErrorMessage, useField, FieldArray } from 'formik';

import { faEdit, faPlus, faTrash } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

import Loading from "../components/Loading";
import styles from "../components/Modal.module.css";
import { convertNotificationToDateTime, apiDateFormat, calendarDateFormat } from '../utils/dates';

const EventDetails = () => {
    // TODO: consider using Portals for a single modal component 
    // - https://legacy.reactjs.org/docs/portals.html
    const apiOrigin = getConfig().apiOrigin;
    const { getAccessTokenSilently } = useAuth0();
    const history = useHistory();
    const params = useParams();
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);
    const [dates, onChange] = useState([new Date(), new Date()]);
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        setIsLoading(true);
        getEventDetails().then((result) => {
            setData(result);
        }).finally(() => setIsLoading(false));
    }, []);

    const MyTextArea = ({ label, ...props }) => {
        const [field, meta] = useField(props);
        return (
            <>
                <label htmlFor={props.id || props.name}>{label}</label>
                <textarea className="text-area d-block mb-2 form-control" {...field} {...props} />
                {meta.touched && meta.error ? (
                    <div className="error">{meta.error}</div>
                ) : null}
            </>
        );
    };

    const getEventDetails = async () => {
        try {
            const token = await getAccessTokenSilently();
            const response = await fetch(`${apiOrigin}/Event/GetEventById/${params.eventid}`, {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            const responseData = await response.json();

            if (response.status !== 200) {
                var errorText = response.status === 500 ? responseData.message : responseData.title;
                setError(errorText);
                throw `${response.status}: An error occurred fetching event details`;
            }

            // date range picker re-initialize
            onChange([responseData.startDate, responseData.endDate]);

            // reinit rest of the form
            let mappedValues = {
                name: responseData.name,
                description: responseData.description,
                location: responseData.location,
                invitees: [],
                notifications: []
            }

            if (responseData.invitees) {
                mappedValues.invitees = responseData.invitees.map((i) => i.inviteeEmailId);
            }

            if (responseData.notifications) {
                mappedValues.notifications = responseData.notifications.map((n) => {
                    const startDateMinutes = (new Date(responseData.startDate)).getTime();
                    const notificationMinutes = (new Date(n.notificationDate)).getTime();
                    var diff = (notificationMinutes - startDateMinutes) / 1000;
                    diff /= 60;
                    return Math.abs(Math.round(diff)).toString();
                })
            }

            return mappedValues;

        } catch (error) {
            console.error(error);
        }
    }

    const updateEvent = async (event) => {
        try {
            const token = await getAccessTokenSilently();

            const body = {
                eventid: params.eventid,
                startDate: apiDateFormat(dates[0]),
                endDate: apiDateFormat(dates[1]),
                name: event.name,
                description: event.description,
                location: event.location,
                timezone: Intl.DateTimeFormat().resolvedOptions().timeZone
            };

            if (event.invitees) {
                body.invitees = event.invitees.map((i) => {
                    return {
                        inviteeEmailId: i
                    }
                });
            }
            if (event.notifications) {
                body.notifications = event.notifications
                    .map((n) => {
                        return {
                            notificationDate: convertNotificationToDateTime(n, dates[0])
                        }
                    });
            }

            const response = await fetch(`${apiOrigin}/Event`, {
                method: 'PUT',
                headers: {
                    Authorization: `Bearer ${token}`,
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(body)
            });

            if (response.status !== 202){
                var responseData = await response.json();
                throw `Error: ${response.statusText} - ${JSON.stringify(responseData.errors)}`;
            }

        } catch (error) {
            throw error;
        }
    };

    const deleteEvent = async () => {
        try {
            const token = await getAccessTokenSilently();

            await fetch(`${apiOrigin}/Event/Delete/${params.eventid}`, {
                method: 'DELETE',
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            history.push('/');

        } catch (error) {
            console.error(error);
        }
    };

    if (isLoading) {
        return <><Loading /></>;
    } else {
        return <>
            {error && <h1 className="text-danger">Error: {error} </h1>}

            {data && (
                <Formik
                    initialValues={data}
                    enableReinitialize={true}
                    validate={values => {
                        let errors = {};
                        const emailRegex = /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i;

                        if (!values.name) {
                            errors.name = 'Required';
                        }

                        if (values.invitees) {
                            errors.invitees = values.invitees.map((email) => {
                                if (!emailRegex.test(email))
                                    return "Invalid email address";
                            });

                            if (errors.invitees.every(element => !element) && !errors.name) {
                                errors = null;
                            }
                        }

                        return errors;
                    }}
                    onSubmit={(values, { setSubmitting }) => {
                        setTimeout(() => {
                            setIsLoading(true);
                            updateEvent(values).then(() => {
                                alert('Event updated successfully!');
                            }, error => {
                                console.error(error);
                                alert('An unexpected error occurred while saving the event');
                            }).finally(() => {
                                setIsLoading(false);
                                setSubmitting(false);
                            });
                        }, 400);
                    }}
                >
                    {({ isSubmitting, values }) => (
                        <Form>
                            <h1 className="mb-2"><FontAwesomeIcon icon={faEdit} /> Manage Event</h1>
                            <div className={styles.modalContent}>
                                <div className="mb-2 w-100 d-flex">
                                    <DateTimeRangePicker required={true} className="pull-left" onChange={onChange} value={dates} format={calendarDateFormat()} />
                                </div>
                                <div className="mb-2 w-100 d-flex">
                                    <Field type="input" name="name" placeholder="Name" className="d-block form-control" />
                                    <ErrorMessage name="name" component="div" className="text-danger" />
                                </div>

                                <div className="mb-2 w-100 d-flex">
                                    <MyTextArea
                                        name="description"
                                        rows="6"
                                        cols="50"
                                        placeholder="Description"
                                    />
                                </div>

                                <div className="mb-2 w-100 d-flex">
                                    <Field type="input" name="location" placeholder="Location" className="mb-2 d-block form-control" />
                                </div>

                                <div className="mb-2 w-100 d-flex">
                                    <FieldArray name="invitees">
                                        {({ push, remove }) => (
                                            <div>
                                                {values.invitees.map((email, index) => (
                                                    <div key={index} className="d-flex">
                                                        <Field name={`invitees.${index}`}
                                                            autoComplete="off"
                                                            className="pull-left form-control d-inline-block"
                                                            placeholder="Invitee email" />
                                                        <ErrorMessage name={`invitees.${index}`} component="div" className="text-danger" />
                                                        <button type="button"
                                                            className="m-1 btn btn-outline-danger btn-sm rounded d-inline-block"
                                                            onClick={() => remove(index)}>
                                                            <FontAwesomeIcon icon={faTrash}></FontAwesomeIcon>
                                                        </button>
                                                    </div>
                                                ))}
                                                <div className="float-left">
                                                    <button type="button" className="mt-1 btn btn-outline-info rounded pull-left" onClick={() => push("")}>
                                                        <FontAwesomeIcon icon={faPlus}></FontAwesomeIcon> Add invitee
                                                    </button>
                                                </div>
                                            </div>
                                        )}
                                    </FieldArray>
                                </div>

                                <div className="mb-2 w-100 d-flex">
                                    <FieldArray name="notifications">
                                        {({ push, remove }) => (
                                            <div>
                                                {values.notifications.map((select, index) => (
                                                    <div key={index} className="d-flex">
                                                        <Field name={`notifications.${index}`}>
                                                            {({ field }) => (
                                                                <select {...field} className="form-control d-inline-block">
                                                                    <option value="0">On time</option>
                                                                    <option value="30">30 mins before</option>
                                                                    <option value="60">1 hour before</option>
                                                                    <option value="120">2 hours before</option>
                                                                </select>
                                                            )}
                                                        </Field>
                                                        <ErrorMessage name={`notifications.${index}`} />
                                                        <button type="button" className="m-1 btn btn-outline-danger btn-sm rounded d-inline-block" onClick={() => remove(index)}>
                                                            <FontAwesomeIcon icon={faTrash}></FontAwesomeIcon>
                                                        </button>
                                                    </div>
                                                ))}
                                                <div className="float-left">
                                                    <button type="button" className="mt-1 btn btn-outline-info rounded" onClick={() => push("0")}>
                                                        <FontAwesomeIcon icon={faPlus}></FontAwesomeIcon> Add notification
                                                    </button>
                                                </div>
                                            </div>
                                        )}
                                    </FieldArray>
                                </div>

                            </div>
                            <div className="d-flex justify-content-center">
                                <div className={styles.actionsContainer}>
                                    <button className="btn btn-info rounded m-2" disabled={isSubmitting}
                                        type="submit">
                                        Save
                                    </button>
                                    <button type="button"
                                        className="btn btn-outline-dark rounded m-2"
                                        onClick={() => { history.push("/") }}
                                    >
                                        Cancel
                                    </button>
                                    <button className="btn btn-outline-danger rounded m-2" disabled={isSubmitting}
                                        type="button" onClick={() => { if (window.confirm('Are you sure you want to delete this event?')) deleteEvent() }}>
                                        Delete
                                    </button>
                                </div>
                            </div>
                        </Form>
                    )}
                </Formik >
            )}
        </>
    }
}


export default withRouter(withAuthenticationRequired(EventDetails, {
    onRedirecting: () => <Loading />,
}));