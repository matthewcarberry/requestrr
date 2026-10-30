
import { useEffect, useState } from "react";
import { useDispatch, useSelector } from 'react-redux';
import { Alert } from "reactstrap";
import { getBookSettings, saveDisabledBookClient, saveReadarrClient } from "../store/actions/BookClientsActions";
import Dropdown from "../components/Inputs/Dropdown"
import Readarr from "../components/DownloadClients/Readarr/Readarr";

// reactstrap components
import {
  Button,
  Card,
  CardHeader,
  CardBody,
  FormGroup,
  Form,
  Container,
  Row,
  Col
} from "reactstrap";
// core components
import UserHeader from "../components/Headers/UserHeader.jsx";


// Settings page for Books (Bookshelf) and Audiobooks (Listenarr), selected with props.section and props.clientName
function Books(props) {
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveAttempted, setSaveAttempted] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [client, setClient] = useState("");
  const [readarr, setReadarr] = useState({});
  const [isReadarrValid, setIsReadarrValid] = useState(false);


  const reduxState = useSelector((state) => {
    return {
      settings: state[props.section]
    };
  });
  const dispatch = useDispatch();


  useEffect(() => {
    dispatch(getBookSettings(props.section))
      .then(data => {
        setIsLoading(false);
        setClient(data.payload.client);
        setReadarr(data.payload.readarr);
      });
  }, []);


  useEffect(() => {
    if (!isSubmitted)
      return;

    if (!isSaving) {
      if ((client === "Disabled"
        || (client === props.clientName
          && isReadarrValid)
      )) {
        setIsSaving(true);

        let saveAction = null;

        if (client === "Disabled") {
          saveAction = dispatch(saveDisabledBookClient(props.section));
        } else if (client === props.clientName) {
          saveAction = dispatch(saveReadarrClient(props.section, {
            readarr: readarr,
            client: props.clientName
          }));
        }

        saveAction.then(data => {
          setIsSaving(false);
          setIsSubmitted(false);

          if (data.ok) {
            setSaveAttempted(true);
            setSaveError("");
            setSaveSuccess(true);
          } else {
            var error = "An unknown error occurred while saving.";

            if (typeof (data.error) === "string")
              error = data.error;

            setSaveAttempted(true);
            setSaveError(error);
            setSaveSuccess(false);
          }
        });
      } else {
        setSaveAttempted(true);
        setSaveError("Some fields are invalid, please fix them before saving.");
        setSaveSuccess(false);
        setIsSubmitted(false);
      }
    }
  }, [isSubmitted]);


  const onClientChange = () => {
    setReadarr(reduxState.settings.readarr);

    setSaveAttempted(false);
    setIsSubmitted(false);
  }

  const onSaving = (e) => {
    e.preventDefault();
    setIsSubmitted(true);
  }




  return (
    <>
      <UserHeader title={props.title} description={props.description} />
      <Container className="mt--7" fluid>
        <Row>
          <Col className="order-xl-1" xl="12">
            <Card className="bg-secondary shadow">
              <CardHeader className="bg-white border-0">
                <Row className="align-items-center">
                  <Col xs="8">
                    <h3 className="mb-0">Configuration</h3>
                  </Col>
                  <Col className="text-right" xs="4">
                    <Button
                      color="primary"
                      href="#pablo"
                      onClick={e => e.preventDefault()}
                      size="sm"
                    >
                      Settings
                    </Button>
                  </Col>
                </Row>
              </CardHeader>
              <CardBody className={isLoading ? "fade" : "fade show"}>
                <Form className="complex">
                  <h6 className="heading-small text-muted mb-4">
                    General Settings
                  </h6>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <Dropdown
                          name="Download Client"
                          value={client}
                          items={[{ name: "Disabled", value: "Disabled" }, { name: props.clientName, value: props.clientName }]}
                          onChange={newClient => { setClient(newClient); onClientChange(); }}
                        />
                      </Col>
                    </Row>
                    <Row>
                      <Col lg="6">
                        {
                          reduxState.settings.client !== client && reduxState.settings.client !== "Disabled" ?
                            <Alert className="text-center" color="warning">
                              <strong>Changing the download client will delete all pending notifications for this page.</strong>
                            </Alert>
                            : null
                        }
                      </Col>
                    </Row>
                  </div>
                  {
                    client !== "Disabled"
                      ? <>
                        <hr className="my-4" />
                        {
                          client === props.clientName ?
                            <>
                              <Readarr
                                section={props.section}
                                clientName={props.clientName}
                                onChange={values => setReadarr(values)}
                                onValidate={value => setIsReadarrValid(value)}
                                isSubmitted={isSubmitted}
                                isSaving={isSaving}
                              />
                            </>
                            : null
                        }
                      </>
                      : null
                  }
                  <div className="pl-lg-4">
                    <Row>
                      <Col>
                        <FormGroup className="mt-4">
                          {
                            saveAttempted && !isSaving ?
                              !saveSuccess ? (
                                <Alert className="text-center" color="danger">
                                  <strong>{saveError}</strong>
                                </Alert>)
                                : <Alert className="text-center" color="success">
                                  <strong>Settings updated successfully.</strong>
                                </Alert>
                              : null
                          }
                        </FormGroup>
                        <FormGroup className="text-right">
                          <button className="btn btn-icon btn-3 btn-primary" onClick={onSaving} disabled={isSaving} type="button">
                            <span className="btn-inner--icon"><i className="fas fa-save"></i></span>
                            <span className="btn-inner--text">Save Changes</span>
                          </button>
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                </Form>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default Books;