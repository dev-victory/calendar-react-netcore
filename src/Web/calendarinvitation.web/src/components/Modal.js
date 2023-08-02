import React, { useState } from "react";
import styles from "./Modal.module.css";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWindowClose } from '@fortawesome/free-solid-svg-icons';
import { useAuth0 } from "@auth0/auth0-react";
import moment from "moment";

import DateTimeRangePicker from '@wojtekmaj/react-datetimerange-picker';
import '@wojtekmaj/react-datetimerange-picker/dist/DateTimeRangePicker.css';
import 'react-calendar/dist/Calendar.css';
import 'react-clock/dist/Clock.css';

import { Formik, Form, Field, ErrorMessage, useField, FieldArray } from 'formik';

// TODO: consider using Portals for a single modal component 
// - https://legacy.reactjs.org/docs/portals.html
const apiOrigin = "http://localhost:5020";

const MyTextArea = ({ label, ...props }) => {
  // useField() returns [formik.getFieldProps(), formik.getFieldMeta()]
  // which we can spread on <input> and alse replace ErrorMessage entirely.
  const [field, meta] = useField(props);
  return (
    <>
      <label htmlFor={props.id || props.name}>{label}</label>
      <textarea className="text-area d-block mb-2" {...field} {...props} />
      {meta.touched && meta.error ? (
        <div className="error">{meta.error}</div>
      ) : null}
    </>
  );
};


const Modal = ({ setIsOpen, eventData }) => {
  const { getAccessTokenSilently } = useAuth0();
  const [value, onChange] = useState([eventData.start, eventData.end]);
  const momentobj = moment;

  const postEvent = async (event) => {

    try {
      const token = await getAccessTokenSilently();

      const body = {
        startDate: apiDateFormat(value[0]),
        endDate: apiDateFormat(value[1]),
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
          // remove duplicates
          .reduce((acc, curr) => {
            if (!acc.includes(curr))
              acc.push(curr);
            return acc;
          }, [])
          .map((n) => {
            return {
              notificationDate: convertNotificationToDateTime(n, value[0])
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
      console.error(error);
    }
  };

  const convertNotificationToDateTime = (minutesToAdd, startDate) => {
    var date = new Date(startDate);
    date.setMinutes((date.getMinutes()) - (parseInt(minutesToAdd)))
    return apiDateFormat(date.toString());
  }

  const apiDateFormat = (jsDate) => {
    return moment.parseZone(jsDate).format("YYYY-MM-DDTHH:mm:ss");
  }

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
              invitees: [""]
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
                  });
              }, 400);
            }}
          >
            {({ isSubmitting, values }) => (
              <Form>
                <div className={styles.modalContent}>
                  <div className="mb-2">
                    <DateTimeRangePicker required={true} onChange={onChange} value={value} />
                  </div>

                  <Field type="input" name="name" placeholder="Name" className="d-block" />
                  <ErrorMessage name="name" component="div" />

                  <MyTextArea
                    name="description"
                    rows="6"
                    cols="50"
                    placeholder="Description"
                  />

                  <Field type="input" name="location" placeholder="Location" className="mb-2 d-block" />

                  <FieldArray name="invitees">
                    {({ push, remove }) => (
                      <div>
                        {values.invitees.map((email, index) => (
                          <div key={index} className={styles.multiAddContainer}>
                            <Field name={`invitees.${index}`}
                              autoComplete="off"
                              className="mb-2 pull-left"
                              placeholder="Invitee email" />
                            <ErrorMessage name={`invitees.${index}`} />
                            <button type="button" className="m-1" onClick={() => remove(index)}>
                              Remove
                            </button>
                          </div>
                        ))}
                        <button type="button" className="mb-2" onClick={() => push("")}>
                          Add another email
                        </button>
                      </div>
                    )}
                  </FieldArray>

                  <FieldArray name="notifications">
                    {({ push, remove }) => (
                      <div>
                        {values.notifications.map((select, index) => (
                          <div key={index} className={styles.multiAddContainer}>
                            <Field name={`notifications.${index}`}>
                              {({ field }) => (
                                <select {...field}>
                                  <option value="0">On time</option>
                                  <option value="30">30 mins before</option>
                                  <option value="60">1 hour before</option>
                                  <option value="120">2 hours before</option>
                                </select>
                              )}
                            </Field>
                            <ErrorMessage name={`notifications.${index}`} />
                            <button type="button" className="m-1" onClick={() => remove(index)}>
                              Remove
                            </button>
                          </div>
                        ))}
                        <button type="button" className="mt-1" onClick={() => push("0")}>
                          Add notification
                        </button>
                      </div>
                    )}
                  </FieldArray>


                </div>
                <div className={styles.modalActions}>
                  <div className={styles.actionsContainer}>
                    <button className={styles.deleteBtn} disabled={isSubmitting}
                      type="submit">
                      Schedule
                    </button>
                    <button
                      className={styles.cancelBtn}
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