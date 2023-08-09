import React, { useState } from "react";
import styles from "./Modal.module.css";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWindowClose, faTrash, faPlus } from '@fortawesome/free-solid-svg-icons';
import { useAuth0 } from "@auth0/auth0-react";

import DateTimeRangePicker from '@wojtekmaj/react-datetimerange-picker';
import '@wojtekmaj/react-datetimerange-picker/dist/DateTimeRangePicker.css';
import 'react-calendar/dist/Calendar.css';
import 'react-clock/dist/Clock.css';

import { convertNotificationToDateTime, apiDateFormat } from '../utils/dates';

import { Formik, Form, Field, ErrorMessage, useField, FieldArray } from 'formik';

// TODO: consider using Portals for a single modal component 
// - https://legacy.reactjs.org/docs/portals.html
const apiOrigin = "http://localhost:5020";

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

const Modal = ({ setIsOpen, eventData, setCalendarReload }) => {
  const { getAccessTokenSilently } = useAuth0();
  const start = apiDateFormat(eventData.start);
  const end = apiDateFormat(eventData.end);

  const [dates, onChange] = useState([start, end]);

  const postEvent = async (event) => {

    try {
      const token = await getAccessTokenSilently();

      const body = {
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
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Accept': 'application/json',
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(body)
      });

      const responseData = await response.json();

      console.log(responseData);
    } catch (error) {
      throw error;
    }
  };

  return (
    <>
      <div className={styles.darkBG} onClick={() => setIsOpen(false)} />
      <div className={styles.centered}>
        <div className={styles.modal}>
          <div className={styles.modalHeader}>
            <h5 className={styles.heading}>New Event</h5>
          </div>
          <button className={styles.closeBtn} onClick={() => setIsOpen(false)}>
            <FontAwesomeIcon icon={faWindowClose} />
          </button>
          <Formik
            initialValues={{
              name: '',
              description: '',
              location: '',
              notifications: [],
              invitees: []
            }}

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
                postEvent(values)
                  .then(() => {
                    setSubmitting(false);
                    setIsOpen(false);
                    setCalendarReload(true);
                  }, error => {
                    console.error(error);
                    alert('An error occurred while saving the event');
                  });
              }, 400);
            }}
          >
            {({ isSubmitting, values }) => (
              <Form>
                <div className={styles.modalContent}>
                  <div className="mb-2">
                    <DateTimeRangePicker required={true} onChange={onChange} value={dates} className="w-100" format={'dd MMM yyyy hh:mm a'} />
                  </div>

                  <Field type="input" name="name" placeholder="Name" className="d-block form-control" />
                  <ErrorMessage name="name" component="div" className="text-danger" />

                  <MyTextArea
                    name="description"
                    rows="6"
                    cols="50"
                    placeholder="Description"
                  />

                  <Field type="input" name="location" placeholder="Location" className="mb-2 d-block form-control" />

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
                            <button type="button" className="m-1 btn btn-danger btn-sm rounded d-inline-block" onClick={() => remove(index)}>
                              <FontAwesomeIcon icon={faTrash}></FontAwesomeIcon>
                            </button>
                          </div>
                        ))}
                        <button type="button" className="m-1 btn btn-outline-info btn-sm rounded" onClick={() => push("")}>
                          <FontAwesomeIcon icon={faPlus}></FontAwesomeIcon> Add invitee
                        </button>
                      </div>
                    )}
                  </FieldArray>

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
                            <button type="button" className="m-1 btn btn-danger btn-sm rounded d-inline-block" onClick={() => remove(index)}>
                              <FontAwesomeIcon icon={faTrash}></FontAwesomeIcon>
                            </button>
                          </div>
                        ))}
                        <button type="button" className="mt-1 btn btn-outline-info btn-sm rounded" onClick={() => push("0")}>
                          <FontAwesomeIcon icon={faPlus}></FontAwesomeIcon> Add notification
                        </button>
                      </div>
                    )}
                  </FieldArray>


                </div>
                <div className={styles.modalActions}>
                  <div className="d-flex justify-content-center">
                    <button className="btn btn-primary m-2 rounded" disabled={isSubmitting}
                      type="submit">
                      Schedule
                    </button>
                    <button
                      className="btn btn-outline-dark m-2 rounded"
                      onClick={() => setIsOpen(false)}>
                      Cancel
                    </button>
                  </div>
                </div>
              </Form>
            )}
          </Formik>
        </div>
      </div>
    </>
  );
};

export default Modal;