import React, { useState } from "react";
import styles from "./Modal.module.css";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWindowClose } from '@fortawesome/free-solid-svg-icons';

import DateTimeRangePicker from '@wojtekmaj/react-datetimerange-picker';
import '@wojtekmaj/react-datetimerange-picker/dist/DateTimeRangePicker.css';
import 'react-calendar/dist/Calendar.css';
import 'react-clock/dist/Clock.css';

import { Formik, Form, Field, ErrorMessage, useField, FieldArray } from 'formik';

// TODO: consider using Portals for a single modal component 
// - https://legacy.reactjs.org/docs/portals.html

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

  const [value, onChange] = useState([eventData.start, eventData.end]);
  //const { inputFields, setInputFields } = useState(initialFormValues);

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
              notifications: [""],
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
                alert(JSON.stringify(values, null, 2));
                setSubmitting(false);
                setIsOpen(false);
              }, 400);
            }}
          >
            {({ isSubmitting, values, setValues }) => (
              <Form>
                <div className={styles.modalContent}>
                  <div className="mb-2">
                    <DateTimeRangePicker required={true} onChange={onChange} value={value} />
                  </div>

                  <Field type="input" name="name" placeholder="Event name" className="d-block" />
                  <ErrorMessage name="name" component="div" />

                  <MyTextArea
                    name="description"
                    rows="6"
                    cols="50"
                    placeholder="Enter event description"
                  />

                  <Field type="input" name="location" placeholder="Event location" className="mb-2 d-block" />

                  <FieldArray name="invitees">
                    {({ push, remove }) => (
                      <div className="mb-2">
                        {values.invitees.map((email, index) => (
                          <div key={index}>
                            <Field name={`invitees.${index}`} autoComplete="off" className="mb-2" placeholder="Invitee email address" />
                            <ErrorMessage name={`invitees.${index}`} />
                            <button type="button" onClick={() => remove(index)}>
                              Remove
                            </button>
                          </div>
                        ))}
                        <button type="button" onClick={() => push("")}>
                          Add another email
                        </button>
                      </div>
                    )}
                  </FieldArray>

                  <FieldArray name="notifications">
                    {({ push, remove }) => (
                      <div>
                        {values.notifications.map((select, index) => (
                          <div key={index}>
                            <Field name={`notifications.${index}`} render={({ field }) => (
                              <select {...field}>
                                <option value="">Please select</option>
                                <option value="0">30 mins</option>
                                <option value="1">1 hour</option>
                                <option value="2">2 hours</option>
                              </select>
                            )} />
                            <ErrorMessage name={`notifications.${index}`} />
                            <button type="button" onClick={() => remove(index)}>
                              Remove
                            </button>
                          </div>
                        ))}
                        <button type="button" onClick={() => push("")}>
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