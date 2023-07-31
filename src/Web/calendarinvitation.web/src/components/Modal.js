import React from "react";
import styles from "./Modal.module.css";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faWindowClose } from '@fortawesome/free-solid-svg-icons';

const Modal = ( {setIsOpen, eventData} ) => {
    

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
          <div className={styles.modalContent}>
            Add new events here {eventData.start} - {eventData.end}
          </div>
          <div className={styles.modalActions}>
            <div className={styles.actionsContainer}>
              <button className={styles.deleteBtn} onClick={() => setIsOpen(false)}>
                Schedule
              </button>
              <button
                className={styles.cancelBtn}
                onClick={() => setIsOpen(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default Modal;